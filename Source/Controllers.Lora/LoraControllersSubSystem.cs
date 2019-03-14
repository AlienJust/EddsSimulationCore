﻿using System;
using System.Collections.Generic;
using System.IO;
using AJ.Std.Composition;
using AJ.Std.Composition.Contracts;
using AJ.Std.Loggers;
using AJ.Std.Loggers.Contracts;
using AJ.Std.Text;
using AJ.Std.Text.Contracts;
using Audience;
using Controllers.Gateway;
using Controllers.Gateway.Attached;
using PollServiceProxy.Contracts;
using PollSystem.CommandManagement.Channels;

namespace Controllers.Lora
{
    public class LoraControllersSubSystem : CompositionPartBase, ISubSystem
    {
        private static readonly ILogger Log = new RelayMultiLogger(true, new RelayLogger(Env.GlobalLog, new ChainedFormatter(new ITextFormatter[] {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})), new RelayLogger(new ColoredConsoleLogger(ConsoleColor.Black, ConsoleColor.Yellow), new ChainedFormatter(new ITextFormatter[] {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})));


        private ICompositionPart _scadaPollGatewayPart;
        private IPollGateway _scadaPollGateway;


        private ICompositionPart _attachedControllersInfoSystemPart;
        private IAttachedControllersInfoSystem _attachedControllersInfoSystem;

        private ICompositionPart _gatewayControllesManagerPart;
        private IGatewayControllerInfosSystem _gatewayControllesManager;


        private ICompositionRoot _compositionRoot;
        private IEnumerable<LoraControllerInfoSimple> _loraControllerInfos;
        private IList<LoraControllerFullInfo> _loraControllers;

        public string SystemName => "LoraControllers";

        private string _mqttTopicStart;
        private MqttDriver _mqttDriver;

        private readonly string _mqttBrokerHost = "127.0.0.1"; // TODO: Move to config file
        private readonly int _mqttBrokerPort = 1883; // std port is 1883 TCP  // TODO: Move to config file

        private IChannelCommandManagerSystemSide<string> _commandManagerSystemSide;

        public LoraControllersSubSystem()
        {
            Log.Log("Called .ctor");
        }

        public override void SetCompositionRoot(ICompositionRoot root)
        {
            _compositionRoot = root;

            _attachedControllersInfoSystemPart = _compositionRoot.GetPartByName("GatewayAttachedControllers");
            _attachedControllersInfoSystem = _attachedControllersInfoSystemPart as IAttachedControllersInfoSystem;
            if (_attachedControllersInfoSystem == null)
                throw new Exception("Не удалось найти GatewayAttachedControllers через composition root");
            _attachedControllersInfoSystemPart.AddRef();

            _gatewayControllesManagerPart = _compositionRoot.GetPartByName("GatewayControllers");
            _gatewayControllesManager = _gatewayControllesManagerPart as IGatewayControllerInfosSystem;
            if (_gatewayControllesManager == null)
                throw new Exception("Не удалось найти GatewayControllers через composition root");
            _gatewayControllesManagerPart.AddRef();


            _scadaPollGatewayPart = _compositionRoot.GetPartByName("PollGateWay");
            _scadaPollGateway = _scadaPollGatewayPart as IPollGateway;
            if (_scadaPollGateway == null)
                throw new Exception("Не удалось найти PollGateWay через composition root");
            _scadaPollGatewayPart.AddRef();
            _scadaPollGateway.RegisterSubSystem(this);


            var commandManager = new InteleconCommandManager<string>();
            _commandManagerSystemSide = commandManager;

            Log.Log("Background worker Inited OK");

            _loraControllerInfos = XmlFactory.GetObjectsConfigurationsFromXml(Path.Combine(Env.CfgPath, "LoraControllerInfos.xml"));
            _mqttTopicStart = "application/1/node/";
            _loraControllers = new List<LoraControllerFullInfo>();
            // need to create full info about controllers:
            Log.Log("Creating full information for each lora controller...");
            foreach (var loraControllerInfo in _loraControllerInfos)
            {
                Log.Log("Lora object: " + loraControllerInfo.Name + ":");
                var rxTopicName = _mqttTopicStart + loraControllerInfo.DeviceId + "/rx";
                var txTopicName = _mqttTopicStart + loraControllerInfo.DeviceId + "/tx";
                var attachedControllerConfig = _attachedControllersInfoSystem.GetAttachedControllerConfigByName(loraControllerInfo.Name);

                var fullLoraConfig = new LoraControllerFullInfo(loraControllerInfo, rxTopicName, txTopicName, attachedControllerConfig);
                _loraControllers.Add(fullLoraConfig);
                Log.Log(fullLoraConfig);
            }

            if (_loraControllers.Count > 0)
            {
                _mqttDriver = new MqttDriver(_mqttBrokerHost, _mqttBrokerPort, _loraControllers, commandManager);
            }

            Log.Log("Lora controllers subsystem was loaded! Built _loraControllers count = " + _loraControllers.Count);
        }


        public void ReceiveData(
            string uplinkName,
            string subObjectName,
            byte commandCode,
            IReadOnlyList<byte> data,
            Action notifyOperationComplete,
            Action<int, IReadOnlyList<byte>> sendReplyAction)
        {
            var isLoraControllerFound = false; // if found - контроллер должен выполнить вызов notifyOperationComplete
            try
            {
                Log.Log("Received data request for object: " + subObjectName + ", command code is: " + commandCode + ", data bytes are: " + data.ToText());

                if (commandCode == 6 && data.Count >= 8)
                {
                    var channel = data[0];
                    var type = data[1];
                    var number = data[2];
                    Log.Log("[OK] Command code and data length are correct, att_channel=" + channel + ", att_type=" + type + ", att_number=" + number);

                    if (type != 49 && type != 50 && type != 51 && type != 52 && type != 53)
                    {
                        Log.Log("[ER] Att_type is not in range [49:53], LORA controllers subsystem does not handling such (" + type + ") att_type, return");
                        return;
                    }

                    try
                    {
                        var loraControllerFullInfo = FindLoraController(subObjectName, type, channel, number);
                        isLoraControllerFound = true;
                        Log.Log(
                            "[OK] - Such LORA controller found in configs, generating command and pushing it to command manager, controller ID is: " +
                            loraControllerFullInfo.LoraControllerInfo.Name);
                        
                        var cmd = new InteleconAnyCommand(Guid.NewGuid().ToString(), commandCode, data);
                        _commandManagerSystemSide.AcceptRequestCommandForSending(
                            loraControllerFullInfo.LoraControllerInfo.Name, cmd, CommandPriority.Normal,
                            TimeSpan.FromSeconds(180), (exc, reply) =>
                            {
                                try
                                {
                                    if (exc != null) throw exc;
                                    if (reply != null)
                                    {
                                        Log.Log("-----------  Driver exc is null, sending reply back  -----------");
                                        Log.Log("-----------  Reply.Code: " + reply.Code + "   Reply.Data: " + reply.Data.ToText());
                                        sendReplyAction((byte) reply.Code, reply.Data);
                                    }
                                    else
                                    {
                                        Log.Log("-----------  ERROR IN PROGRAM: exc == null && reply == null!");
                                        throw new Exception("Error in algorithm");
                                    }
                                }
                                catch (Exception e)
                                {
                                    Log.Log("-----------------------  При обработке ответа от объекта LORA возникло исключение: " + e);
                                }
                                finally
                                {
                                    notifyOperationComplete(); // выполняется в другом потоке
                                }
                            });
                        Log.Log("[OK] Command was pushed to command manager, timeout = 180 sec");
                    }
                    catch (AttachedControllerNotFoundException)
                    {
                        Log.Log("[OK] Such LORA controller was NOT FOUND in configs!");
                        //notifyOperationComplete();
                    }
                    catch (Exception ex)
                    {
                        Log.Log(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Log("Произошла ошибка во время работы с полученными данными: " + ex);
            }
            finally
            {
                if (!isLoraControllerFound)
                {
                    Log.Log("[OK] Such LORA controller was NOT FOUND in configs, lets notify upper system by calling back notifyOperationComplete();");
                    notifyOperationComplete();
                }
            }
        }

        public override string Name => "LoraControllers";

        public override void BecameUnused()
        {
            _scadaPollGatewayPart.Release();
            _attachedControllersInfoSystemPart.Release();
            _gatewayControllesManagerPart.Release();
        }

        private LoraControllerFullInfo FindLoraController(string gatewayName, byte type, byte channel, byte number)
        {
            foreach (var loraControllerFullInfo in _loraControllers)
            {
                //Log.Log("Checking obj: " + loraControllerFullInfo.LoraControllerInfo.Name + " > " + loraControllerFullInfo.AttachedControllerConfig);
                if (loraControllerFullInfo.AttachedControllerConfig.Gateway == gatewayName &&
                    loraControllerFullInfo.AttachedControllerConfig.Type == type &&
                    loraControllerFullInfo.AttachedControllerConfig.Channel == channel &&
                    loraControllerFullInfo.AttachedControllerConfig.Number == number)
                {
                    return loraControllerFullInfo;
                }
            }

            throw new AttachedControllerNotFoundException();
        }
    }
}