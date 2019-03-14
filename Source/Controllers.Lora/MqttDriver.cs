using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using AJ.Std.Concurrent;
using AJ.Std.Concurrent.Contracts;
using AJ.Std.Loggers;
using AJ.Std.Loggers.Contracts;
using AJ.Std.Text;
using AJ.Std.Text.Contracts;
using Audience;
using Controllers.Gateway.Attached;
using Controllers.Lora.JsonBrocaar;
using nMqtt;
using nMqtt.Messages;
using Newtonsoft.Json;
using PollSystem.CommandManagement.Channels;
using PollSystem.CommandManagement.Channels.Exceptions;

namespace Controllers.Lora
{
    internal sealed class MqttDriver
    {
        private static readonly ILogger Log = new RelayMultiLogger(true,
            new RelayLogger(Env.GlobalLog,
                new ChainedFormatter(new ITextFormatter[]
                    {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})),
            new RelayLogger(new ColoredConsoleLogger(ConsoleColor.Black, ConsoleColor.Cyan),
                new ChainedFormatter(new ITextFormatter[]
                    {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})));


        private readonly string _mqttBrokerHost;
        private readonly int _tcpPort;

        private readonly IAttachedLastDataCache _lastSixsCache;


        private readonly IList<LoraControllerFullInfo> _loraControllers;
        private IChannelCommandManagerDriverSide<string> _commandManagerDriverSide;

        private readonly AutoResetEvent _prevSubscribeIsComplete;
        private readonly ManualResetEvent _initComplete;
        private Exception _initException;

        private readonly IWorker<Action> _backWorker;
        private readonly MqttClient _mqttClient;

        public MqttDriver(string mqttBrokerHost, int tcpPort, IList<LoraControllerFullInfo> loraControllers, IChannelCommandManagerDriverSide<string> commandManagerDriverSide)
        {
            _mqttBrokerHost = mqttBrokerHost;
            _tcpPort = tcpPort;
            _loraControllers = loraControllers;
            _commandManagerDriverSide = commandManagerDriverSide;
            _commandManagerDriverSide.CommandRequestAccepted += CommandManagerDriverSideOnCommandRequestAccepted;

            _prevSubscribeIsComplete = new AutoResetEvent(false);
            _initComplete = new ManualResetEvent(false);
            _initException = null;


            _lastSixsCache = new AttachedLastDataCache();

            _backWorker = new SingleThreadedRelayQueueWorkerProceedAllItemsBeforeStopNoLog<Action>("MqttDrv_Bw", a => a(), ThreadPriority.BelowNormal, true, null);

            _mqttClient = new MqttClient(_mqttBrokerHost, Guid.NewGuid().ToString()) {Port = _tcpPort};
            _mqttClient.SomeMessageReceived += OnMessageReceived;
            _ = _mqttClient.ConnectAsync();
            Log.Log("[MQTT DRIVER] Connecting to MQTT broker...");

            _initComplete.WaitOne(TimeSpan.FromSeconds(10.0));
            if (_initException != null)
                throw _initException;
            Log.Log("[MQTT DRIVER] .ctor complete");
        }

        private void CommandManagerDriverSideOnCommandRequestAccepted(object sender, CommandRequestAcceptedEventArgs<string> ea)
        {
            AcceptRequest(ea.ObjectId);
        }

        private void AcceptRequest(string loraObjectName)
        {
            Log.Log("[MQTT DRIVER ACCEPT REQUEST] called");
            try
            {
                //IInteleconCommand cmd;
                while(true)
                {
                    var cmd = _commandManagerDriverSide.NextCommandForDriver(loraObjectName);
                    if (cmd != null)
                    {
                        Log.Log("[MQTT DRIVER ACCEPT REQUEST] " + cmd.Identifier + " > Command is taken from command manager");
                        if (cmd.Code == 6 && cmd.Data.Count >= 8)
                        {
                            var channel = cmd.Data[0];
                            var type = cmd.Data[1];
                            var number = cmd.Data[2];
                            var config = cmd.Data[3];

                            try
                            {
                                var loraControllerFullInfo = FindControllerByAttachedInfo(type, channel, number);
                                // TAKE DATA FROM CACHE:
                                if (config < 8)
                                {
                                    Log.Log("[MQTT DRIVER ACCEPT REQUEST] " + cmd.Identifier + " > Config = " + config + ", taking data from cache");
                                    // taking data from cache, if exist and time is less than cache ttl
                                    var data = _lastSixsCache.GetData(loraObjectName, config);
                                    if (DateTime.Now - data.Item1 < TimeSpan.FromSeconds(loraControllerFullInfo.LoraControllerInfo.DataTtl))
                                    {
                                        Log.Log("[MQTT DRIVER ACCEPT REQUEST] " + cmd.Identifier + " > Data in cache is good, sending it back as 6 reply, data: " + cmd.Data.Take(8).ToText());
                                        _commandManagerDriverSide.ReceiveSomeReplyCommandFromDriver(loraObjectName, new InteleconAnyCommand(Guid.NewGuid().ToString(), 16, data.Item2));
                                    }
                                    else
                                    {
                                        Log.Log("[MQTT DRIVER ACCEPT REQUEST] " + cmd.Identifier + " > Data in cache too old, sending empty 6 reply with data: " + cmd.Data.Take(8).ToText());
                                        _commandManagerDriverSide.ReceiveSomeReplyCommandFromDriver(loraObjectName, new InteleconAnyCommand(Guid.NewGuid().ToString(), 16, cmd.Data.Take(8).ToList()));
                                    }
                                }
                                // PUSH DATA TO MQTT TOPIC:
                                else
                                {
                                    Log.Log("[MQTT DRIVER ACCEPT REQUEST] " + cmd.Identifier + " > Config = " + config + ", need to publish message to MQTT channel");

                                    var dataBeginStr = "{\"reference\": \"SCADA-edds\", \"confirmed\": true, \"fPort\": 2, \"data\": \"";
                                    var dataItself = PackInteleconCommand(cmd, loraControllerFullInfo.LoraControllerInfo.InteleconNetAddress); // TODO: think about taking controller InteleconNetAddress from gateway
                                    Log.Log("[MQTT DRIVER ACCEPT REQUEST] " + cmd.Identifier + " > Data to pack to base64: " + dataItself.ToText());
                                    var strBase64 = Convert.ToBase64String(dataItself);
                                    var dataEndStr = "\"}";
                                    var textData = dataBeginStr + strBase64 + dataEndStr;
                                    Log.Log(loraControllerFullInfo.TxTopicName);
                                    Log.Log(textData);
                                    _mqttClient.Publish(loraControllerFullInfo.TxTopicName, Encoding.UTF8.GetBytes(textData), Qos.AtLeastOnce);

                                    Log.Log("[MQTT DRIVER ACCEPT REQUEST] " + cmd.Identifier + " > Data were published to MQTT topic");
                                }
                            }
                            catch (AttachedControllerNotFoundException)
                            {
                                Log.Log("[MQTT DRIVER ACCEPT REQUEST] Attached controller was not found! Replying empty package with data: " + cmd.Data.Take(8).ToText());
                                _commandManagerDriverSide.ReceiveSomeReplyCommandFromDriver(loraObjectName, new InteleconAnyCommand(Guid.NewGuid().ToString(), 16, cmd.Data.Take(8).ToList()));
                            }
                            catch (CannotGetDataFromCacheException)
                            {
                                Log.Log("[MQTT DRIVER ACCEPT REQUEST] No data in cache! Replying empty package with data: " + cmd.Data.Take(8).ToText());
                                _commandManagerDriverSide.ReceiveSomeReplyCommandFromDriver(loraObjectName, new InteleconAnyCommand(Guid.NewGuid().ToString(), 16, cmd.Data.Take(8).ToList()));
                            }
                        }
                        else
                        {
                            Log.Log("[MQTT DRIVER ACCEPT REQUEST] Unknown CMD code = " + cmd.Code + " received! Invoking notification that reply will not be sent");
                            _commandManagerDriverSide.LastCommandsReplyWillNotBeReceived(loraObjectName);
                        }
                    }
                    else
                    {
                        Log.Log("[MQTT DRIVER ACCEPT REQUEST] No more commands to take for MQTT driver");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Log("[MQTT DRIVER ACCEPT REQUEST] ERROR, ex: " + ex);
            }
        }

        private static byte[] PackInteleconCommand(IInteleconCommand cmd, int inteleconNetworkAddress)
        {
            return cmd.Data.ToArray().GetNetBuffer((ushort) inteleconNetworkAddress, (byte) cmd.Code);
        }

        private void OnMessageReceived(MqttMessage message)
        {
            switch (message)
            {
                case ConnAckMessage msg:
                    Log.Log("[MQTT DRIVER OnMqttMessageReceived] ---- OnConnAck");
                    _backWorker.AddWork(() =>
                    {
                        try
                        {
                            if (msg.ConnectReturnCode == ConnectReturnCode.ConnectionAccepted)
                            {
                                Log.Log("[MQTT DRIVER OnMqttMessageReceived] Connected OK");
                                if (_loraControllers == null) Log.Log("_loraControllers is null!!!");
                                foreach (var loraControllerInfo in _loraControllers)
                                {
                                    Log.Log("[MQTT DRIVER OnMqttMessageReceived] Subscribing for topic: " + loraControllerInfo.RxTopicName);
                                    Log.Log("[MQTT DRIVER OnMqttMessageReceived] Waiting for SubscribeAckMessage from MQTT broker...");
                                    _mqttClient.Subscribe(loraControllerInfo.RxTopicName, Qos.AtLeastOnce);

                                    // DEBUG:
                                    /*loraController.WhenPublishMessageReceived(Encoding.UTF8.GetBytes("{\"applicationID\":\"1\",\"applicationName\":\"mgf_vega_nucleo_debug_app\",\"deviceName\":\"mgf\",\"devEUI\":\"be7a0000000000c8\",\"deviceStatusBattery\":254,\"deviceStatusMargin\":26,\"rxInfo\":[{\"mac\":\"0000e8eb11417531\",\"time\":\"2018-07-05T10:20:46.12777Z\",\"rssi\":-46,\"loRaSNR\":7.2,\"name\":\"vega-gate\",\"latitude\":55.95764,\"longitude\":60.57098,\"altitude\":317}],\"txInfo\":{\"frequency\":868500000,\"dataRate\":{\"modulation\":\"LORA\",\"bandwidth\":125,\"spreadFactor\":7},\"adr\":true,\"codeRate\":\"4/5\"},\"fCnt\":2502,\"fPort\":2,\"data\":\"/////w==\"}"));*/

                                    _prevSubscribeIsComplete.WaitOne(TimeSpan.FromMinutes(0.1));
                                    Log.Log("[MQTT DRIVER OnMqttMessageReceived]Subscribed for topic" + loraControllerInfo.RxTopicName + " OK");
                                }
                            }
                            else
                            {
                                Log.Log("[MQTT DRIVER OnMqttMessageReceived] Connection error");
                                throw new Exception("Cannot connect to MQTT broker");
                            }
                        }
                        catch (Exception exception)
                        {
                            Log.Log("[MQTT DRIVER OnMqttMessageReceived] Exception during _backWorker.AddWork(() => { called on conAck.. }: " + exception);
                            _initException = new Exception("Cannot init MQTT", exception);
                        }
                        finally
                        {
                            _initComplete.Set();
                        }
                    });
                    break;

                case SubscribeAckMessage msg:
                    Log.Log("[MQTT DRIVER OnMqttMessageReceived] ---- OnSubAck");
                    _prevSubscribeIsComplete.Set();
                    break;

                case PublishMessage msg:
                    Log.Log("[MQTT DRIVER OnMqttMessageReceived] ---- OnMessageReceived > PublishMessage received from MQTT broker");
                    Console.WriteLine(@"topic:{0} data:{1}", msg.TopicName, Encoding.UTF8.GetString(msg.Payload));

                    try
                    {
                        Log.Log("[MQTT DRIVER OnMqttMessageReceived] Received RX " + msg.TopicName + " >>> " + msg.Payload.ToText());
                        var rawJson = Encoding.UTF8.GetString(msg.Payload);
                        Log.Log("[MQTT DRIVER OnMqttMessageReceived] Parsed RX >>> " + rawJson);
                        var suchTopicControllers = _loraControllers.Where(lc => lc.RxTopicName == msg.TopicName).ToList();
                        if (suchTopicControllers.Count > 0)
                        {
                            var parsedJson = JsonConvert.DeserializeObject<MqttBrocaarMessage>(rawJson);
                            Log.Log("[MQTT DRIVER OnMqttMessageReceived] Parsed fPort = " + parsedJson.Fport);
                            Log.Log("[MQTT DRIVER OnMqttMessageReceived] Parsed fCnt = " + parsedJson.Fcnt);

                            var lastData = parsedJson.Data;
                            Log.Log("[MQTT DRIVER OnMqttMessageReceived] Parsed RX LAST DATA: >>> " + lastData);
                            // handles even several "metadata controllers": o_O
                            var selfControllers = suchTopicControllers.Where(lc => lc.AttachedControllerConfig.Type == 49);
                            foreach (var fullControllerInfo in selfControllers)
                            {
                                var loraMetadata = new byte[40];
                                loraMetadata[0] = (byte) fullControllerInfo.AttachedControllerConfig.Channel;
                                loraMetadata[1] = (byte) fullControllerInfo.AttachedControllerConfig.Type;
                                loraMetadata[2] = (byte) fullControllerInfo.AttachedControllerConfig.Number;
                                loraMetadata[3] = 0; // config is current;
                                loraMetadata[4] = (byte) DateTime.Now.Hour;
                                loraMetadata[5] = (byte) DateTime.Now.Day;
                                loraMetadata[6] = (byte) DateTime.Now.Month;
                                loraMetadata[7] = (byte) DateTime.Now.Year;

                                loraMetadata[8] = (byte) parsedJson.DeviceStatusBattery;
                                loraMetadata[9] = (byte) parsedJson.Fport;

                                float rxLatitude;
                                float rxLongitude;
                                float rxAltitude;
                                float rxLoraSnr;
                                short rxRssi;

                                var rxInfo = parsedJson.RxInfo.FirstOrDefault();
                                if (rxInfo == null)
                                {
                                    Log.Log("[MQTT DRIVER OnMqttMessageReceived] RX INFO HAS NO ITEMS!");
                                    rxLatitude = 0f;
                                    rxLongitude = 0f;
                                    rxAltitude = 0f;
                                    rxLoraSnr = 0f;
                                    rxRssi = 0;
                                }
                                else
                                {
                                    Log.Log("[MQTT DRIVER OnMqttMessageReceived] RX INFO exist");
                                    rxLatitude = (float) rxInfo.Latitude;
                                    rxLongitude = (float) rxInfo.Longitude;
                                    rxAltitude = rxInfo.Altitude;
                                    rxLoraSnr = (float) rxInfo.LoraSnr;
                                    rxRssi = (short) rxInfo.Rssi;
                                }

                                BitConverter.GetBytes(rxLatitude).CopyTo(loraMetadata, 10);
                                BitConverter.GetBytes(rxLongitude).CopyTo(loraMetadata, 14);
                                BitConverter.GetBytes(rxAltitude).CopyTo(loraMetadata, 18);
                                BitConverter.GetBytes(rxLoraSnr).CopyTo(loraMetadata, 22);
                                BitConverter.GetBytes(rxRssi).CopyTo(loraMetadata, 26);
                                //Log.Log("RX INFO was added to array");


                                BitConverter.GetBytes(parsedJson.TxInfo.Frequency).CopyTo(loraMetadata, 28);
                                //Log.Log("TX INFO frequency was added to array");
                                BitConverter.GetBytes((short) parsedJson.TxInfo.DataRate.Bandwidth).CopyTo(loraMetadata, 32);
                                //Log.Log("TX INFO DataRate.Bandwidth was added to array");
                                BitConverter.GetBytes((short) parsedJson.TxInfo.DataRate.SpreadFactor).CopyTo(loraMetadata, 34);
                                //Log.Log("TX INFO DataRate.SpreadFactor was added to array");
                                BitConverter.GetBytes(parsedJson.Fcnt).CopyTo(loraMetadata, 36);
                                //Log.Log("fCnt was added to array");

                                _lastSixsCache.AddData(fullControllerInfo.LoraControllerInfo.Name, 0, loraMetadata); // lora controller is always online, if we received something from MQTT
                                Log.Log("[MQTT DRIVER OnMqttMessageReceived] For LORA METADATA controller with name = " + fullControllerInfo.LoraControllerInfo.Name + " data was added to cache");
                            }

                            // SOME OTHER COUNTER TYPE (technology: Karat, self, etc):
                            var receivedData = Convert.FromBase64String(lastData);
                            Log.Log("[MQTT DRIVER OnMqttMessageReceived] Decoded bytes are: " + receivedData.ToText());

                            // Intelecon2 micropacket:
                            if (receivedData.Length == 4)
                            {
                                if (receivedData[0] == 0x71)
                                {
                                    var netAddr = (ushort) (receivedData[2] + (receivedData[1] << 8)); // I'm not really need this net address, cause I know, from witch topic data were taken
                                }
                            }
                            // full Intelecon packet
                            else if (receivedData.Length >= 8)
                            {
                                Log.Log("[MQTT DRIVER OnMqttMessageReceived] Received data len is more then 8");
                                var netAddr = (ushort) (receivedData[4] + (receivedData[3] << 8)); // I'm not really need this net address, cause I know, from witch topic data were taken
                                var cmdCode = receivedData[2];
                                Log.Log("[MQTT DRIVER OnMqttMessageReceived] CommandCode=" + cmdCode);
                                if (cmdCode == 16)
                                {
                                    Log.Log("[MQTT DRIVER OnMqttMessageReceived] InteleconNetAddr=" + netAddr);
                                    var rcvData = new byte[receivedData.Length - 8];
                                    for (int i = 0; i < rcvData.Length; ++i)
                                    {
                                        rcvData[i] = receivedData[i + 5];
                                    }

                                    Log.Log("[MQTT DRIVER OnMqttMessageReceived] rcvData: " + rcvData.ToText());
                                    if (rcvData.Length >= 8)
                                    {
                                        var channel = rcvData[0];
                                        var type = rcvData[1];
                                        var number = rcvData[2];
                                        var config = rcvData[3];
                                        Log.Log("ch=" + channel + ", type=" + type + ", number=" + number + ", config=" + config);
                                        var loraController = suchTopicControllers.FirstOrDefault(lc => lc.AttachedControllerConfig.Channel == channel && lc.AttachedControllerConfig.Type == type && lc.AttachedControllerConfig.Number == number); // TAKING only first (or nothing)
                                        if (loraController != null)
                                        {
                                            Log.Log("[MQTT DRIVER OnMqttMessageReceived] Such lora controller was found, its name is " + loraController.LoraControllerInfo.Name);
                                            if (config < 8)
                                            {
                                                Log.Log("[MQTT DRIVER OnMqttMessageReceived] Config is less than 8 - saving data to cache");
                                                _lastSixsCache.AddData(loraController.LoraControllerInfo.Name, config, rcvData);
                                            }
                                            else
                                            {
                                                Log.Log("[MQTT DRIVER OnMqttMessageReceived] Config is greater or equals 8 - notifying system about answer from MQTT channel");
                                                // all the others commands works as normal
                                                _commandManagerDriverSide.ReceiveSomeReplyCommandFromDriver(loraController.LoraControllerInfo.Name, new InteleconAnyCommand(Guid.NewGuid().ToString(), cmdCode, rcvData));
                                            }
                                            Log.Log("[MQTT DRIVER OnMqttMessageReceived] Trying to reaccept REQUEST (if any)");
                                            AcceptRequest(loraController.LoraControllerInfo.Name); // after receiving good command trying to work with more accepted commands instantly
                                        }
                                        else
                                        {
                                            Log.Log("[MQTT DRIVER OnMqttMessageReceived] Cannot find lora controller with such channel, type, number");
                                        }
                                    }
                                    else
                                    {
                                        Log.Log("[MQTT DRIVER OnMqttMessageReceived] Reply is InteleconAttached, but preInfo.Length is less than 8");
                                    }
                                }
                                else
                                {
                                    Log.Log("[MQTT DRIVER OnMqttMessageReceived] Heared from MQTT Intelecon reply's command code is not 16");
                                }
                            }
                            else Log.Log("[MQTT DRIVER OnMqttMessageReceived] Data bytes count too low, it cannot be Intelecon command");
                        }
                        else
                        {
                            Log.Log("[MQTT DRIVER OnMqttMessageReceived] No lora controllers with such RxTopicName were found");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Log(ex);
                    }

                    break;
            }
        }

        private LoraControllerFullInfo FindControllerByAttachedInfo(byte type, byte channel, byte number)
        {
            foreach (var loraControllerFullInfo in _loraControllers)
            {
                if (loraControllerFullInfo.AttachedControllerConfig.Type == type && loraControllerFullInfo.AttachedControllerConfig.Channel == channel && loraControllerFullInfo.AttachedControllerConfig.Number == number)
                    return loraControllerFullInfo;
            }

            throw new AttachedControllerNotFoundException();
        }
    }
}