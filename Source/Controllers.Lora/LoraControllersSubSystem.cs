using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Text;
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
using Controllers.Gateway;
using Controllers.Gateway.Attached;
using Controllers.Lora.JsonBrocaar;
using nMqtt;
using nMqtt.Messages;
using Newtonsoft.Json;
using PollServiceProxy.Contracts;
using PollSystem.CommandManagement.Channels;

namespace Controllers.Lora {
	[Export(typeof(ICompositionPart))]
	public class LoraControllersSubSystem : CompositionPartBase, ISubSystem {
		private static readonly ILogger Log = new RelayMultiLogger(true, new RelayLogger(Env.GlobalLog, new ChainedFormatter(new ITextFormatter[] { new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ") })), new RelayLogger(new ColoredConsoleLogger(ConsoleColor.Black, ConsoleColor.Yellow), new ChainedFormatter(new ITextFormatter[] { new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ") })));


		private ICompositionPart _scadaPollGatewayPart;
		private IPollGateway _scadaPollGateway;


		private ICompositionPart _attachedControllersInfoSystemPart;
		private IAttachedControllersInfoSystem _attachedControllersInfoSystem;

		private ICompositionPart _gatewayControllesManagerPart;
		private IGatewayControllerInfosSystem _gatewayControllesManager;


		private ICompositionRoot _compositionRoot;
		private readonly IEnumerable<LoraControllerInfoSimple> _loraControllerInfos;
		private IList<LoraControllerFullInfo> _loraControllers;
		private IWorker<Action> _backWorker;

		public string SystemName => "LoraControllers";

		private readonly string _mqttTopicStart;
		private readonly MqttClient _mqttClient;

		private readonly string _mqttBrokerHost = "127.0.0.1"; // TODO: Move to config file
		private readonly int _mqttBrokerPort = 1883; // std port is 1883 TCP  // TODO: Move to config file

		private readonly AutoResetEvent _prevSubscribeIsComplete;
		private readonly ManualResetEvent _initComplete;
		private Exception _initException;

		private IChannelCommandManagerDriverSide<string> _commandManagerDriverSide;
		private IChannelCommandManagerSystemSide<string> _commandManagerSystemSide;

		public LoraControllersSubSystem() {
			var commandManager = new InteleconCommandManager<string>();
			_commandManagerDriverSide = commandManager;
			_commandManagerSystemSide = commandManager;

			_commandManagerDriverSide.CommandRequestAccepted += CommandManagerDriverSideOnCommandRequestAccepted;

			_backWorker = new SingleThreadedRelayQueueWorkerProceedAllItemsBeforeStopNoLog<Action>("Lora (mqtt) background worker", a => a(), ThreadPriority.BelowNormal, true, null);
			Log.Log("Background worker Inited OK");
			
			_prevSubscribeIsComplete = new AutoResetEvent(false);
			_initComplete = new ManualResetEvent(false);
			_initException = null;

			dynamic serializer = new JsonSerializer();

			_loraControllerInfos = XmlFactory.GetObjectsConfigurationsFromXml(Path.Combine(Env.CfgPath, "LoraControllerInfos.xml"));

			_mqttClient = new MqttClient(_mqttBrokerHost, Guid.NewGuid().ToString()) { Port = _mqttBrokerPort };

			_mqttClient.SomeMessageReceived += OnMessageReceived;
			_mqttClient.ConnectAsync();

			_mqttTopicStart = "application/1/node/";


			Log.Log("Waits until all RX topics would be subscribed...");
			_initComplete.WaitOne();
			if (_initException != null)
				throw _initException;
			Log.Log(".ctor complete");
		}

		public override void SetCompositionRoot(ICompositionRoot root) {
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

			_loraControllers = new List<LoraControllerFullInfo>();
			// need to create full info about controllers:
			Log.Log("Creating full information for each lora controller...");
			foreach (var loraControllerInfo in _loraControllerInfos) {
				var rxTopicName = _mqttTopicStart + loraControllerInfo.DeviceId + "/rx";
				var txTopicName = _mqttTopicStart + loraControllerInfo.DeviceId + "/tx";
				var attachedControllerConfig = _attachedControllersInfoSystem.GetAttachedControllerConfigByName(loraControllerInfo.Name);
				_loraControllers.Add(new LoraControllerFullInfo(loraControllerInfo, rxTopicName, txTopicName, attachedControllerConfig));
			}

			Log.Log("Lora controllers subsystem was loaded! Built _loraControllers count = " + _loraControllers.Count);
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
					Log.Log("---- OnMessageReceived");
					Console.WriteLine(@"topic:{0} data:{1}", msg.TopicName, Encoding.UTF8.GetString(msg.Payload));

					try {
						LoraControllerFullInfo info = FindLoraControllerInfoByRxTopic(msg.TopicName);

						Log.Log("Received rx " + msg.TopicName + " >>> " + msg.Payload.ToText());
						// TODO: reply to scada

						var rawJson = Encoding.UTF8.GetString(msg.Payload);
						Log.Log("Parsed RX >>> " + rawJson);

						var parsedJson = JsonConvert.DeserializeObject<MqttBrocaarMessage>(rawJson);
						Log.Log("Parsed fPort = " + parsedJson.Fport);
						Log.Log("Parsed fCnt = " + parsedJson.Fcnt);

						var lastData = parsedJson.Data;
						//var lastData = rawJson.Split(",\"data\":").Last();
						//lastData = lastData.Substring(1, lastData.Length - 3);
						Log.Log("Parsed RX LAST DATA: >>> " + lastData);
						var receivedData = Convert.FromBase64String(lastData);
						Log.Log("Decoded bytes are: " + receivedData.ToText());

						// TODO: check if decoded bytes are inteleconCommand
						if (receivedData.Length >= 8) {
							var netAddr = (ushort)(receivedData[4] + (receivedData[3] << 8)); // I'm not really need this net address :)
							var cmdCode = receivedData[2];
							var rcvData = new byte[receivedData.Length - 8];
							for (int i = 0; i < rcvData.Length; ++i) {
								rcvData[i] = receivedData[i + 5];
							}

							Log.Log("Invoked data received event");
							_commandManagerDriverSide.ReceiveSomeReplyCommandFromDriver(info.LoraControllerInfo.Name, new InteleconAnyCommand(123, cmdCode, rcvData));
						}
					}
					catch (AttachedControllerNotFoundException) {
						Log.Log("Attached LORA controller not found by MQTT topic: " + msg.TopicName);
					}

					break;
			}
		}


		private void CommandManagerDriverSideOnCommandRequestAccepted(string loraObjectName) {
			try {
				var cmd = _commandManagerDriverSide.NextCommandForDriver(loraObjectName);
				if (cmd != null) {
					if (cmd.Code == 6 && cmd.Data.Count >= 8) {
						var channel = cmd.Data[0];
						var type = cmd.Data[1];
						var number = cmd.Data[2];

						try {
							var controller = FindControllerByAttachedInfo(type, channel, number);
							var dataBeginStr = "{\"reference\": \"SCADA-edds\", \"confirmed\": true, \"fPort\": 2, \"data\": \"";
							var dataItself = PackInteleconCommand(cmd, controller.LoraControllerInfo.InteleconNetAddress);
							var strBase64 = Convert.ToBase64String(dataItself);
							var dataEndStr = "\"}";

							_mqttClient.Publish(controller.TxTopicName, Encoding.UTF8.GetBytes(dataBeginStr + strBase64 + dataEndStr));
							// TODO: after publishing command to MQTT need to wait?
						}
						catch (AttachedControllerNotFoundException) {
							// TODO: log
						}
					}
					else
						_commandManagerDriverSide.LastCommandReplyWillNotBeReceived(loraObjectName, cmd);
				}
			}
			catch (Exception e) {
				Console.WriteLine(e);
				throw;
			}
		}


		private static byte[] PackInteleconCommand(IInteleconCommand cmd, int inteleconNetworkAddress) {
			return cmd.Data.ToArray().GetNetBuffer((ushort)inteleconNetworkAddress, (byte)cmd.Code);
		}


		private LoraControllerFullInfo FindControllerByAttachedInfo(byte type, byte channel, byte number) {
			foreach (var loraControllerFullInfo in _loraControllers) {
				if (loraControllerFullInfo.AttachedControllerConfig.Type == type && loraControllerFullInfo.AttachedControllerConfig.Channel == channel && loraControllerFullInfo.AttachedControllerConfig.Number == number)
					return loraControllerFullInfo;
			}

			throw new AttachedControllerNotFoundException();
		}


		private LoraControllerFullInfo FindLoraControllerInfoByRxTopic(string msgTopicName) {
			foreach (var loraControllerFullInfo in _loraControllers) {
				if (loraControllerFullInfo.RxTopicName == msgTopicName)
					return loraControllerFullInfo;
			}

			throw new AttachedControllerNotFoundException();
		}


		public void ReceiveData(string uplinkName, string subObjectName, byte commandCode, IReadOnlyList<byte> data, Action notifyOperationComplete, Action<int, IReadOnlyList<byte>> sendReplyAction) {
			var isLoraControllerFound = false; // if found - контроллер должен выполнить вызов notifyOperationComplete
			try {
				Log.Log("Received data request for object: " + subObjectName + ", command code is: " + commandCode + ", data bytes are: " + data.ToText());

				if (commandCode == 6 && data.Count >= 8) {
					var channel = data[0];
					var type = data[1];
					var number = data[2];
					Log.Log("Command code and data length are correct, att_channel=" + channel + ", att_type=" + type + ", att_number=" + number);

					if (type != 50) {
						Log.Log("Att_type != 50, LORA controllers subsystem does not handling such (" + type + ") att_type, return");
						return;
					}

					foreach (var loraControllerFullInfo in _loraControllers) {
						if (loraControllerFullInfo.AttachedControllerConfig.Gateway == subObjectName && channel == loraControllerFullInfo.AttachedControllerConfig.Channel && type == loraControllerFullInfo.AttachedControllerConfig.Type && number == loraControllerFullInfo.AttachedControllerConfig.Number) {
							isLoraControllerFound = true;
							var cmd = new InteleconAnyCommand(123, commandCode, data); // 123 is sample ID
							_commandManagerSystemSide.AcceptRequestCommandForSending(loraControllerFullInfo.LoraControllerInfo.Name, cmd, CommandPriority.Normal, TimeSpan.FromSeconds(30), (exc, reply) => {
								try {
									sendReplyAction((byte)reply.Code, reply.Data);
								}
								catch (Exception e) {
									Log.Log("При обработке ответа от объекта LORA возникло исключение: " + e);
								}
								finally {
									notifyOperationComplete(); // выполняется в другом потоке
								}
							});
							break;
						}
					}
				}
			}
			catch (Exception ex) {
				Log.Log("Произошла ошибка во время работы с полученными данными: " + ex);
			}
			finally {
				if (!isLoraControllerFound) {
					notifyOperationComplete();
				}
			}
		}

		public override string Name => "LoraControllers";

		public override void BecameUnused() {
			_scadaPollGatewayPart.Release();
			_attachedControllersInfoSystemPart.Release();
			_gatewayControllesManagerPart.Release();
		}
	}
}