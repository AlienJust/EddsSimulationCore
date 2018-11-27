using System;
using System.Collections.Generic;
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
using PollSystem.CommandManagement.Channels.Exceptions;

namespace Controllers.Lora {
	public class LoraControllersSubSystem : CompositionPartBase, ISubSystem {
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

		public LoraControllersSubSystem() {
			Log.Log("Called .ctor");
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


			var commandManager = new InteleconCommandManager<string>();
			_commandManagerSystemSide = commandManager;

			Log.Log("Background worker Inited OK");

			_loraControllerInfos = XmlFactory.GetObjectsConfigurationsFromXml(Path.Combine(Env.CfgPath, "LoraControllerInfos.xml"));
			_mqttTopicStart = "application/1/node/";
			_loraControllers = new List<LoraControllerFullInfo>();
			// need to create full info about controllers:
			Log.Log("Creating full information for each lora controller...");
			foreach (var loraControllerInfo in _loraControllerInfos) {
				var rxTopicName = _mqttTopicStart + loraControllerInfo.DeviceId + "/rx";
				var txTopicName = _mqttTopicStart + loraControllerInfo.DeviceId + "/tx";
				var attachedControllerConfig = _attachedControllersInfoSystem.GetAttachedControllerConfigByName(loraControllerInfo.Name);
				var fullLoraConfig = new LoraControllerFullInfo(loraControllerInfo, rxTopicName, txTopicName, attachedControllerConfig);
				_loraControllers.Add(fullLoraConfig);
				Log.Log(fullLoraConfig);
			}
			
			_mqttDriver = new MqttDriver(_mqttBrokerHost, _mqttBrokerPort, _loraControllers, commandManager);
			
			Log.Log("Lora controllers subsystem was loaded! Built _loraControllers count = " + _loraControllers.Count);
		}


		public void ReceiveData(string uplinkName, string subObjectName, byte commandCode, IReadOnlyList<byte> data, Action notifyOperationComplete, Action<int, IReadOnlyList<byte>> sendReplyAction) {
			var isLoraControllerFound = false; // if found - контроллер должен выполнить вызов notifyOperationComplete
			try {
				Log.Log("Received data request for object: " + subObjectName + ", command code is: " + commandCode + ", data bytes are: " + data.ToText());

				if (commandCode == 6 && data.Count >= 8) {
					var channel = data[0];
					var type = data[1];
					var number = data[2];
					Log.Log("[OK] Command code and data length are correct, att_channel=" + channel + ", att_type=" + type + ", att_number=" + number);

					if (type != 50) {
						Log.Log("[ER] Att_type != 50, LORA controllers subsystem does not handling such (" + type + ") att_type, return");
						return;
					}

					foreach (var loraControllerFullInfo in _loraControllers) {
						if (loraControllerFullInfo.AttachedControllerConfig.Gateway == subObjectName && channel == loraControllerFullInfo.AttachedControllerConfig.Channel && type == loraControllerFullInfo.AttachedControllerConfig.Type && number == loraControllerFullInfo.AttachedControllerConfig.Number) {
							isLoraControllerFound = true;
							Log.Log("[OK] - Such LORA controller found in configs, generating command and pushing it to command manager, controller ID is: " + loraControllerFullInfo.LoraControllerInfo.Name);
							var cmd = new InteleconAnyCommand(123, commandCode, data); // 123 is sample ID
							_commandManagerSystemSide.AcceptRequestCommandForSending(loraControllerFullInfo.LoraControllerInfo.Name, cmd, CommandPriority.Normal, TimeSpan.FromSeconds(65), (exc, reply) => {
								try {
									if (exc != null) throw exc; 
									if (reply != null) {
										Log.Log("-----------------------  Driver exc is null, sending reply back:");
										Log.Log("-----------------------  Reply.Data: " + reply.Data.ToText());
										Log.Log("-----------------------  Reply.Code: " + reply.Code);
										sendReplyAction((byte) reply.Code, reply.Data);
									}
									else {
										Log.Log("-----------------------  ERROR IN PROGRAM: exc == null && reply == null!");
										throw new Exception("Error in algorythm");
									}
								}
								catch (Exception e) {
									Log.Log("-----------------------  При обработке ответа от объекта LORA возникло исключение: " + e);
								}
								finally {
									notifyOperationComplete(); // выполняется в другом потоке
								}
							});
							Log.Log("[OK] Command was pushed to command manager, breaking search lora object cycle");
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
					Log.Log("[OK] Such LORA controller was NOT FOUND in configs!");
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