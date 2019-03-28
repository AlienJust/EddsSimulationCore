using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using AJ.Std.Composition;
using AJ.Std.Composition.Contracts;
using AJ.Std.Concurrent;
using AJ.Std.Concurrent.Contracts;
using AJ.Std.Loggers;
using AJ.Std.Loggers.Contracts;
using AJ.Std.Text;
using AJ.Std.Text.Contracts;
using Audience;
using PollServiceProxy.Contracts;
using ScadaClient.Contracts;

namespace PollServiceProxy
{
    public sealed class InteleconGateway : CompositionPartBase, IInteleconGateway, ISubSystemRegistrationPoint
    {
        private static readonly ILogger Log = new RelayMultiLogger(true, new RelayLogger(Env.GlobalLog, new ChainedFormatter(new ITextFormatter[] {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})), new RelayLogger(new ColoredConsoleLogger(ConsoleColor.Cyan, Console.BackgroundColor), new ChainedFormatter(new ITextFormatter[] {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})));

        private readonly Dictionary<string, INamedScadaLink> _scadaClients;
        private readonly Dictionary<string, IScadaObjectInfo> _scadaObjects;
        private readonly List<ISubSystem> _internalSystems;
        private readonly Thread _microPacketSendThread;
        private readonly int _microPacketSendingIntervalMs;
        private ICompositionRoot _compositionRoot;

        private readonly Dictionary<IScadaAddress, IWorker<Action>> _perScadaAddressWorkers;

        //private readonly Dictionary<IScadaAddress, WaitableCounter> _perScadaAddressSendMicroPacketsSyncObjs;
        private WaitableMultiCounter<IScadaAddress> _perScadaAddressSendMicroPacketsSyncObjs;

        public InteleconGateway()
        {
            _perScadaAddressWorkers = new Dictionary<IScadaAddress, IWorker<Action>>();
            //_perScadaAddressSendMicroPacketsSyncObjs = new Dictionary<IScadaAddress, object>();
            _perScadaAddressSendMicroPacketsSyncObjs = new WaitableMultiCounter<IScadaAddress>();

            _internalSystems = new List<ISubSystem>();

            _scadaClients = XmlFactory.GetScadaLinksFromXml(Path.Combine(Env.CfgPath, "Servers.xml"));
            _scadaObjects = XmlFactory.GetScadaObjectsFromXml(Path.Combine(Env.CfgPath, "ScadaObjects.xml"));
            _microPacketSendingIntervalMs = XmlFactory.GetMicroPacketSendingIntervalMsFromXml(Path.Combine(Env.CfgPath, "PollServiceProxy.xml"));

            _microPacketSendThread = new Thread(SendMicroPackets);
        }

        public override void SetCompositionRoot(ICompositionRoot root)
        {
            _compositionRoot = root;

            if (_microPacketSendThread.ThreadState == ThreadState.Unstarted)
            {
                foreach (var scadaObjectInfo in _scadaObjects)
                {
                    foreach (var scadaAddress in scadaObjectInfo.Value.ScadaAddresses)
                    {
                        // TODO: strategy selection:
                        // TODO: Add to XML configuration
                        // TODO: strategy: if working then drop 
                        // TODO:    or   : if working then enqueue [ USING this strategy NOW ]
                        _perScadaAddressWorkers.Add(scadaAddress, new SingleThreadedRelayQueueWorkerProceedAllItemsBeforeStopNoLog<Action>(scadaAddress.ToString(), a =>
                        {
                            Log.Log("Executing in object's " + scadaAddress.ToString() + " thread action");
                            try
                            {
                                a();
                            }
                            catch (Exception exception)
                            {
                                Log.Log(exception);
                            }
                        }, ThreadPriority.BelowNormal, true, null));
                        //_perScadaAddressSendMicroPacketsSyncObjs.Add(scadaAddress, new object());
                    }
                }

                foreach (var scadaClient in _scadaClients)
                {
                    scadaClient.Value.DataReceived += OnScadaLinkDataReceived;
                }

                _microPacketSendThread.Start();
                Log.Log("PollGateway.SetCompositionRoot is complete OK");
            }
            else
            {
                Log.Log("PollGateway.SetCompositionRoot something strange, micro-packets send thread was already started! [ER]");
            }
        }


        private void OnScadaLinkDataReceived(object sender, DataReceivedEventArgs eventArgs)
        {
            try
            {
                var scadaClient = sender as INamedScadaLink;
                if (scadaClient == null)
                {
                    Log.Log("OnScadaLinkDataReceived INamedScadaLink, something wrong, scadaClient is null! [ER]");
                    return;
                }

                var scadaObjectNetAddress = eventArgs.NetAddress;
                foreach (var scadaObjectInfo in _scadaObjects)
                {
                    foreach (var objectScadaAddress in scadaObjectInfo.Value.ScadaAddresses)
                    {
                        if (objectScadaAddress.LinkName == scadaClient.Name && objectScadaAddress.NetAddress == eventArgs.NetAddress)
                        {
                            var scadaObjectName = scadaObjectInfo.Key;
                            Log.Log("Object with address " + objectScadaAddress + " was found, need to notify subsystem");
                            _perScadaAddressWorkers[objectScadaAddress].AddWork(() =>
                            {
                                // Increasing counter to prevent micropackets sending.
                                foreach (var internalSystem in _internalSystems)
                                {
                                    try
                                    {
                                        _perScadaAddressSendMicroPacketsSyncObjs.IncrementCount(objectScadaAddress);
                                        Log.Log("Notifying internal ISubSystem with name = " + internalSystem.SystemName);

                                        internalSystem.ReceiveData(scadaClient.Name, scadaObjectName,
                                            eventArgs.CommandCode, eventArgs.Data,
                                            () => _perScadaAddressSendMicroPacketsSyncObjs.DecrementCount(
                                                objectScadaAddress),
                                            (code, reply) => SendReplyData(scadaClient.Name, scadaObjectNetAddress,
                                                (byte) code, reply.ToArray()));
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Log("Something wrong during internal systems notification: " + ex);
                                        // Decrementing on any exception to prevent forever waiting
                                        _perScadaAddressSendMicroPacketsSyncObjs.DecrementCount(objectScadaAddress);
                                    }
                                }

                                Log.Log("All internal systems were notified");
                                //counter.WaitForCounterChangeWhileNotPredecate(c => c <= 0);
                                //Log.Log("All internal systems reported back about notify");
                            });
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Log("PollGateway.OnScadaLinkDataReceived exception: " + ex);
            }
        }

        private void SendMicroPackets()
        {
            while (true)
            {
                int sendsCount = 0;
                foreach (var obj in _scadaObjects)
                {
                    if (obj.Value.SendMicroPackets)
                    {
                        foreach (var target in obj.Value.ScadaAddresses)
                        {
                            var internalSubSystemsWorkingWithObjectCount = _perScadaAddressSendMicroPacketsSyncObjs.GetCount(target);
                            //lock (_perScadaAddressSendMicroPacketsSyncObjs[target])
                            if (internalSubSystemsWorkingWithObjectCount <= 0)
                            {
                                try
                                {
                                    _scadaClients[target.LinkName].Link.SendMicroPacket((ushort) target.NetAddress, 22);
                                    sendsCount++;
                                }
                                catch (Exception ex)
                                {
                                    Log.Log("Exception on sending micropacket: " + ex);
                                    Log.Log(ex.ToString());
                                }
                            }
                            else
                            {
                                Log.Log("Micropacket was not sent to " + target.LinkName +
                                        " because count of subsystems working with object is " +
                                        internalSubSystemsWorkingWithObjectCount);
                            }
                        }
                    }
                }

                Log.Log("Micropackets were sent, count=" + sendsCount);
                Thread.Sleep(_microPacketSendingIntervalMs);
            }
        }

        public void SendDataInstantly(string scadaObjectName, byte commandCode, byte[] data)
        {
            var scadaObjectInfo = _scadaObjects[scadaObjectName];
            Log.Log("Sending INSTANTLY reply for object " + scadaObjectName + ", command code = " + commandCode + " data = " + data.ToText());
            foreach (var scadaAddress in scadaObjectInfo.ScadaAddresses)
            {
                _perScadaAddressWorkers[scadaAddress].AddWork(() => SendReplyData(scadaAddress.LinkName, (ushort) scadaAddress.NetAddress, commandCode, data));
                Log.Log("Sended reply to SCADA system " + scadaAddress.LinkName + " as Intelecon Network Address=" + scadaAddress.NetAddress);
            }
        }

        private void SendReplyData(string uplinkName, ushort netAddress, byte commandCode, byte[] data)
        {
            Log.Log("Need to send data: " + data.ToText() + " to " + uplinkName + " with InteleconNetAddress=" + netAddress + " with commandCode=" + commandCode);
            _scadaClients[uplinkName].Link.SendData(netAddress, commandCode, data);
        }

        public override string Name => "PollGateWay";

        public void RegisterSubSystem(ISubSystem subSystem)
        {
            _internalSystems.Add(subSystem);
            Log.Log("Internal system (subsystem) was registred, name: " + subSystem.SystemName);
        }

        public override void BecameUnused()
        {
            // release all compostionparts
        }
    }
}