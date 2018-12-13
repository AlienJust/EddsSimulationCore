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

namespace Controllers.Lora {
	internal sealed class MqttDriver {
		private static readonly ILogger Log = new RelayMultiLogger(true, new RelayLogger(Env.GlobalLog, new ChainedFormatter(new ITextFormatter[] {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})), new RelayLogger(new ColoredConsoleLogger(ConsoleColor.Black, ConsoleColor.Cyan), new ChainedFormatter(new ITextFormatter[] {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})));


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

		public MqttDriver(string mqttBrokerHost, int tcpPort, IList<LoraControllerFullInfo> loraControllers, IChannelCommandManagerDriverSide<string> commandManagerDriverSide) {
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
			_mqttClient.ConnectAsync();
			Log.Log("Connecting to MQTT broker...");

			_initComplete.WaitOne(TimeSpan.FromSeconds(10.0));
			if (_initException != null)
				throw _initException;
			Log.Log(".ctor complete");
		}

		private void CommandManagerDriverSideOnCommandRequestAccepted(string loraObjectName) {
			try {
				var cmd = _commandManagerDriverSide.NextCommandForDriver(loraObjectName);
				Log.Log("[OK] Command is taken by MQTT driver, or some command was replied and another one was taken instantly");
				if (cmd != null) {
					if (cmd.Code == 6 && cmd.Data.Count >= 8) {
						var channel = cmd.Data[0];
						var type = cmd.Data[1];
						var number = cmd.Data[2];
						var config = cmd.Data[3];

						try {
							// TAKE DATA FROM CACHE:
							if (config < 8) {
								var cachedConfig = FindControllerOrSubcontroller(type, channel, number);
								Log.Log("Config = " + config + ", taking data from cache");
								// taking data from cache, if exist and time is less than cache ttl
								var data = _lastSixsCache.GetData(loraObjectName, config);
								if (DateTime.Now - data.Item1 < TimeSpan.FromSeconds(cachedConfig.DataTtl)) {
									Log.Log("Data in cache is good, sending it back as 6 reply, data: " + cmd.Data.Take(8).ToText());
									_commandManagerDriverSide.ReceiveSomeReplyCommandFromDriver(loraObjectName, new InteleconAnyCommand(123, 16, data.Item2));
								}
								else {
									Log.Log("Data in cache too old, sending empty 6 reply with data: " + cmd.Data.Take(8).ToText());
									_commandManagerDriverSide.ReceiveSomeReplyCommandFromDriver(loraObjectName, new InteleconAnyCommand(123, 16, cmd.Data.Take(8).ToList()));
								}
							}
							// PUSH DATA TO MQTT TOPIC:
							else {
								var controller = FindControllerByAttachedInfo(type, channel, number);
								
								var dataBeginStr = "{\"reference\": \"SCADA-edds\", \"confirmed\": true, \"fPort\": 2, \"data\": \"";
								var dataItself = PackInteleconCommand(cmd, controller.LoraControllerInfo.InteleconNetAddress); // TODO: think about taking controller InteleconNetAddress from gateway
								Log.Log("Data to pack to base64: " + dataItself.ToText());
								var strBase64 = Convert.ToBase64String(dataItself);
								var dataEndStr = "\"}";
								var textData = dataBeginStr + strBase64 + dataEndStr;
								Log.Log(controller.TxTopicName);
								Log.Log(textData);
								_mqttClient.Publish(controller.TxTopicName, Encoding.UTF8.GetBytes(textData), Qos.AtLeastOnce);
								
								// TODO: after publishing command to MQTT need to wait?
								Log.Log("Data were pushed to MQTT");
							}
						}
						catch (AttachedControllerNotFoundException) {
							Log.Log("Replying empty package with data: " + cmd.Data.Take(8).ToText());
							_commandManagerDriverSide.ReceiveSomeReplyCommandFromDriver(loraObjectName, new InteleconAnyCommand(123, 16, cmd.Data.Take(8).ToList()));
						}
						catch (CannotGetDataFromCacheException) {
							Log.Log("Replying empty package with data: " + cmd.Data.Take(8).ToText());
							_commandManagerDriverSide.ReceiveSomeReplyCommandFromDriver(loraObjectName, new InteleconAnyCommand(123, 16, cmd.Data.Take(8).ToList()));
						}
					}
					else {
						Log.Log("Unknown cmd code = " + cmd.Code + " received, reply will not be sended");
						_commandManagerDriverSide.LastCommandReplyWillNotBeReceived(loraObjectName, cmd);
					}
				}
				else {
					Log.Log("[ER] Command is null");
				}
			}
			catch (NeedGiveCommandBackException) {
				Log.Log("[ER] Need to wait for command from driver before adding new one command!");
			}
		}

		private static byte[] PackInteleconCommand(IInteleconCommand cmd, int inteleconNetworkAddress) {
			return cmd.Data.ToArray().GetNetBuffer((ushort) inteleconNetworkAddress, (byte) cmd.Code);
		}

		private void OnMessageReceived(MqttMessage message) {
			switch (message) {
				case ConnAckMessage msg:
					Log.Log("---- OnConnAck");
					_backWorker.AddWork(() => {
						try {
							if (msg.ConnectReturnCode == ConnectReturnCode.ConnectionAccepted) {
								Log.Log("MQTT connected OK");
								if (_loraControllers == null) Log.Log("_loraControllers is null!!!");
								foreach (var loraControllerInfo in _loraControllers) {
									Log.Log("Subscribing for topic: " + loraControllerInfo.RxTopicName);
									Log.Log("Waiting for SubscribeAckMessage from MQTT broker...");
									_mqttClient.Subscribe(loraControllerInfo.RxTopicName, Qos.AtLeastOnce);

									// DEBUG:
									/*loraController.WhenPublishMessageReceived(Encoding.UTF8.GetBytes("{\"applicationID\":\"1\",\"applicationName\":\"mgf_vega_nucleo_debug_app\",\"deviceName\":\"mgf\",\"devEUI\":\"be7a0000000000c8\",\"deviceStatusBattery\":254,\"deviceStatusMargin\":26,\"rxInfo\":[{\"mac\":\"0000e8eb11417531\",\"time\":\"2018-07-05T10:20:46.12777Z\",\"rssi\":-46,\"loRaSNR\":7.2,\"name\":\"vega-gate\",\"latitude\":55.95764,\"longitude\":60.57098,\"altitude\":317}],\"txInfo\":{\"frequency\":868500000,\"dataRate\":{\"modulation\":\"LORA\",\"bandwidth\":125,\"spreadFactor\":7},\"adr\":true,\"codeRate\":\"4/5\"},\"fCnt\":2502,\"fPort\":2,\"data\":\"/////w==\"}"));*/

									_prevSubscribeIsComplete.WaitOne(TimeSpan.FromMinutes(0.1));
									Log.Log("Subscribed for topic" + loraControllerInfo.RxTopicName + " OK");
								}
							}
							else {
								Log.Log("Connection error");
								throw new Exception("Cannot connect to MQTT broker");
							}
						}
						catch (Exception exception) {
							Log.Log("Exception during _backWorker.AddWork(() => { called on conAck.. }: " + exception);
							_initException = new Exception("Cannot init MQTT", exception);
						}
						finally {
							_initComplete.Set();
						}
					});
					break;

				case SubscribeAckMessage msg:
					Log.Log("---- OnSubAck");
					_prevSubscribeIsComplete.Set();
					break;

				case PublishMessage msg:
					Log.Log("---- OnMessageReceived > PublishMessage received from MQTT broker");
					Console.WriteLine(@"topic:{0} data:{1}", msg.TopicName, Encoding.UTF8.GetString(msg.Payload));

					try {
						var loraSelfInfo = FindLoraControllerInfoByRxTopic(msg.TopicName);

						Log.Log("Received rx " + msg.TopicName + " >>> " + msg.Payload.ToText());
						// TODO: reply to scada

						var rawJson = Encoding.UTF8.GetString(msg.Payload);
						Log.Log("Parsed RX >>> " + rawJson);

						var parsedJson = JsonConvert.DeserializeObject<MqttBrocaarMessage>(rawJson);
						Log.Log("Parsed fPort = " + parsedJson.Fport);
						Log.Log("Parsed fCnt = " + parsedJson.Fcnt);

						var lastData = parsedJson.Data;
						Log.Log("Parsed RX LAST DATA: >>> " + lastData);

						// need to save data as Intelecon six reply from lora controller
						// 
						var loraSelfData = new byte[10];
						loraSelfData[0] = (byte) loraSelfInfo.AttachedControllerConfig.Channel;
						loraSelfData[1] = (byte) loraSelfInfo.AttachedControllerConfig.Type;
						loraSelfData[2] = (byte) loraSelfInfo.AttachedControllerConfig.Number;
						loraSelfData[3] = 0; // config is current;
						loraSelfData[4] = (byte) DateTime.Now.Hour;
						loraSelfData[5] = (byte) DateTime.Now.Day;
						loraSelfData[6] = (byte) DateTime.Now.Month;
						loraSelfData[7] = (byte) DateTime.Now.Year;

						loraSelfData[8] = (byte) parsedJson.DeviceStatusBattery;
						loraSelfData[9] = (byte) parsedJson.Fport;

						_lastSixsCache.AddData(loraSelfInfo.LoraControllerInfo.Name, 0, loraSelfData); // lora controller is allways online, if we received something from MQTT

						var receivedData = Convert.FromBase64String(lastData);
						Log.Log("Decoded bytes are: " + receivedData.ToText());

						// TODO: check if decoded bytes are inteleconCommand
						if (receivedData.Length == 4) {
							if (receivedData[0] == 0x71) {
								var netAddr = (ushort) (receivedData[2] + (receivedData[1] << 8)); // I'm not really need this net address, cause I know, from witch topic data were taken
							}
						}
						else if (receivedData.Length >= 8) {
							var netAddr = (ushort) (receivedData[4] + (receivedData[3] << 8)); // I'm not really need this net address, cause I know, from witch topic data were taken
							var cmdCode = receivedData[2];
							Log.Log("Received data len is more then 8, net addr is " + netAddr + ", commandCode=" + cmdCode);
							var rcvData = new byte[receivedData.Length - 8];
							for (int i = 0; i < rcvData.Length; ++i) {
								rcvData[i] = receivedData[i + 5];
							}

							Log.Log("rcvData: " + rcvData.ToText());
							var subobjectConfig = loraSelfInfo.SubobjectsConfigs.FirstOrDefault(sc => sc.AttachedConfig.Channel == rcvData[0] && sc.AttachedConfig.Type == rcvData[1] && sc.AttachedConfig.Number == rcvData[2]);

							if (subobjectConfig != null) {
								// controller data (command six + 10 - reply) is added to cache
								var config = rcvData[3];
								if (cmdCode == 16 && config < 8) {
									_lastSixsCache.AddData(subobjectConfig.SubControllerInfo.Name, config, rcvData);
								}
								else {
									// all the others commands works as normal
									_commandManagerDriverSide.ReceiveSomeReplyCommandFromDriver(loraSelfInfo.LoraControllerInfo.Name, new InteleconAnyCommand(123, cmdCode, rcvData));
								}

								CommandManagerDriverSideOnCommandRequestAccepted(loraSelfInfo.LoraControllerInfo.Name); // after receiving good command trying to work with more accepted commands instantly
							}
							else Log.Log("Attached subobject with ch=" + rcvData[0] + ", type=" + rcvData[1] + ", num=" + rcvData[2] + " was not found in config" );
						}
						else Log.Log("Data bytes count too low, it cannot be Intelecon command");
					}
					catch (AttachedControllerNotFoundException) {
						Log.Log("Attached LORA controller not found by MQTT topic: " + msg.TopicName);
					}

					break;
			}
		}


		private LoraControllerFullInfo FindLoraControllerInfoByRxTopic(string msgTopicName) {
			foreach (var loraControllerFullInfo in _loraControllers) {
				if (loraControllerFullInfo.RxTopicName == msgTopicName)
					return loraControllerFullInfo;
			}

			throw new AttachedControllerNotFoundException();
		}

		private LoraControllerFullInfo FindControllerByAttachedInfo(byte type, byte channel, byte number) {
			foreach (var loraControllerFullInfo in _loraControllers) {
				if (loraControllerFullInfo.AttachedControllerConfig.Type == type && loraControllerFullInfo.AttachedControllerConfig.Channel == channel && loraControllerFullInfo.AttachedControllerConfig.Number == number)
					return loraControllerFullInfo;
			}

			throw new AttachedControllerNotFoundException();
		}
		
		private ICachedDataControllerConfig FindControllerOrSubcontroller(byte type, byte channel, byte number) {
			foreach (var loraControllerFullInfo in _loraControllers) {
				if (loraControllerFullInfo.AttachedControllerConfig.Type == type && loraControllerFullInfo.AttachedControllerConfig.Channel == channel && loraControllerFullInfo.AttachedControllerConfig.Number == number)
					return loraControllerFullInfo.LoraControllerInfo;
				foreach (var subobjectsConfig in loraControllerFullInfo.SubobjectsConfigs) {
					if (subobjectsConfig.AttachedConfig.Type == type && subobjectsConfig.AttachedConfig.Channel == channel && subobjectsConfig.AttachedConfig.Number == number)
						return subobjectsConfig.SubControllerInfo;
				}
			}

			throw new AttachedControllerNotFoundException();
		}
	}
}