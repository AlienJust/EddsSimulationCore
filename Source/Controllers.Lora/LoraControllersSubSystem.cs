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
using nMqtt;
using nMqtt.Messages;
using PollServiceProxy.Contracts;

namespace Controllers.Lora {
  [Export(typeof(ICompositionPart))]
  public class LoraControllersSubSystem : CompositionPartBase, ISubSystem {
    private static readonly ILogger Log = new RelayMultiLogger(true
      , new RelayLogger(Env.GlobalLog
        , new ChainedFormatter(new ITextFormatter[] {
          new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")
        }))
      , new RelayLogger(new ColoredConsoleLogger(ConsoleColor.Black, ConsoleColor.Yellow)
        , new ChainedFormatter(new ITextFormatter[] {
          new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")
        })));


    private ICompositionPart _scadaPollGatewayPart;
    private IPollGateway _scadaPollGateway;


    private ICompositionPart _attachedControllersInfoSystemPart;
    private IAttachedControllersInfoSystem _attachedControllersInfoSystem;

    private ICompositionPart _gatewayControllesManagerPart;
    private IGatewayControllerInfosSystem _gatewayControllesManager;


    private ICompositionRoot _compositionRoot;
    private readonly IEnumerable<LoraControllerInfoSimple> _loraControllerInfos;
    private readonly Dictionary<string, LoraController> _loraControllersByRxTopicName;
    private IWorker<Action> _backWorker;

    public string SystemName => "LoraControllers";

    private readonly string _mqttTopicStart;
    private readonly MqttClient _mqttClient;

    private readonly string _mqttBrokerHost = "127.0.0.1"; // TODO: Move to config file
    private readonly int _mqttBrokerPort = 1883; // std port is 1883 TCP  // TODO: Move to config file

    private readonly AutoResetEvent _prevSubscribeIsComplete;
    private readonly ManualResetEvent _initComplete;
    private Exception _initException;

    public LoraControllersSubSystem() {
      _backWorker =
        new SingleThreadedRelayQueueWorkerProceedAllItemsBeforeStopNoLog<Action>("Lora (mqtt) background worker"
          , a => a(), ThreadPriority.BelowNormal, true, null);
      _prevSubscribeIsComplete = new AutoResetEvent(false);
      _initComplete = new ManualResetEvent(false);
      _initException = null;

      _loraControllerInfos =
        XmlFactory.GetObjectsConfigurationsFromXml(Path.Combine(Env.CfgPath, "LoraControllerInfos.xml"));
      _loraControllersByRxTopicName = new Dictionary<string, LoraController>();

      foreach (var loraControllerInfo in _loraControllerInfos) {
        var rxTopicName = _mqttTopicStart + loraControllerInfo.DeviceId + "/rx";
        var txTopicName = _mqttTopicStart + loraControllerInfo.DeviceId + "/tx";

        var loraController = new LoraController(loraControllerInfo.Name, txTopicName, Log.Log);
      }

      _mqttClient = new MqttClient(_mqttBrokerHost, Guid.NewGuid().ToString()) {Port = _mqttBrokerPort};

      _mqttClient.SomeMessageReceived += OnMessageReceived;
      _mqttClient.ConnectAsync();

      _mqttTopicStart = "application/1/node/";


      Log.Log("Waits until all RX topics would be subscribed...");
      _initComplete.WaitOne();
      if (_initException != null) throw _initException;
      Log.Log(".ctor complete");
    }

    private void OnMessageReceived(MqttMessage message) {
      switch (message) {
        case ConnAckMessage msg:
          Console.WriteLine("---- OnConnAck");
          _backWorker.AddWork(() => {
            try {
              if (msg.ConnectReturnCode == ConnectReturnCode.ConnectionAccepted) {
                Log.Log("MQTT connected OK");

                _loraControllersByRxTopicName.Clear(); // recreate controllers on each connect?
                foreach (var loraControllerInfo in _loraControllerInfos) {
                  var rxTopicName = _mqttTopicStart + loraControllerInfo.DeviceId + "/rx";
                  var txTopicName = _mqttTopicStart + loraControllerInfo.DeviceId + "/tx";

                  var loraController = new LoraController(loraControllerInfo.Name, txTopicName, Log.Log, _mqttClient);
                  _loraControllersByRxTopicName.Add(rxTopicName, loraController);
                  Log.Log("Subscribing for topic: " + rxTopicName);
                  Log.Log("Waiting for SubscribeAckMessage from MQTT broker...");
                  _mqttClient.Subscribe(rxTopicName, Qos.AtLeastOnce);
                  //_prevSubscribeIsComplete.WaitOne(TimeSpan.FromSeconds(5)); // something wrong if cannot get SubAck

                  // DEBUG:
                  loraController.WhenPublishMessageReceived(Encoding.UTF8.GetBytes(
                    "{\"applicationID\":\"1\",\"applicationName\":\"mgf_vega_nucleo_debug_app\",\"deviceName\":\"mgf\",\"devEUI\":\"be7a0000000000c8\",\"deviceStatusBattery\":254,\"deviceStatusMargin\":26,\"rxInfo\":[{\"mac\":\"0000e8eb11417531\",\"time\":\"2018-07-05T10:20:46.12777Z\",\"rssi\":-46,\"loRaSNR\":7.2,\"name\":\"vega-gate\",\"latitude\":55.95764,\"longitude\":60.57098,\"altitude\":317}],\"txInfo\":{\"frequency\":868500000,\"dataRate\":{\"modulation\":\"LORA\",\"bandwidth\":125,\"spreadFactor\":7},\"adr\":true,\"codeRate\":\"4/5\"},\"fCnt\":2502,\"fPort\":2,\"data\":\"/////w==\"}"));

                  _prevSubscribeIsComplete.WaitOne(TimeSpan.FromMinutes(1.0));
                  Log.Log("Subscribed for topic" + rxTopicName + " OK");
                }
              }
              else {
                Log.Log("Connection error");
                throw new Exception("Cannot connect to MQTT broker");
              }
            }
            catch (Exception exception) {
              _initException = new Exception("Cannot init MQTT", exception);
            }
            finally {
              _initComplete.Set();
            }
          });
          break;

        case SubscribeAckMessage msg:
          Console.WriteLine("---- OnSubAck");
          _prevSubscribeIsComplete.Set();
          break;

        case PublishMessage msg:
          Console.WriteLine("---- OnMessageReceived");
          Console.WriteLine(@"topic:{0} data:{1}", msg.TopicName, Encoding.UTF8.GetString(msg.Payload));
          if (_loraControllersByRxTopicName.ContainsKey(msg.TopicName)) {
            Log.Log("Received rx " + msg.TopicName + " >>> " + msg.Payload.ToText());
            _loraControllersByRxTopicName[msg.TopicName].WhenPublishMessageReceived(msg.Payload);
          }

          break;

        default:
          break;
      }
    }


    public override void SetCompositionRoot(ICompositionRoot root) {
      _compositionRoot = root;

      Log.Log("Подсистема подключаемых контроллеров LORA инициализирована, число контроллеров: " +
              _loraControllersByRxTopicName.Count);

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
      if (_scadaPollGateway == null) throw new Exception("Не удалось найти PollGateWay через composition root");
      _scadaPollGatewayPart.AddRef();
      _scadaPollGateway.RegisterSubSystem(this);
    }


    public void ReceiveData(string uplinkName, string subObjectName, byte commandCode, IReadOnlyList<byte> data
      , Action notifyOperationComplete, Action<int, IReadOnlyList<byte>> sendReplyAction) {
      bool isLoraControllerFound = false; // if found - контроллер должен выполненить вызов notifyOperationComplete
      try {
        Log.Log("Received data request for object: " + subObjectName + ", command code is: " + commandCode +
                ", data bytes are: " + data.ToText());

        if (commandCode == 6 && data.Count >= 8) {
          var channel = data[0];
          var type = data[1];
          var number = data[2];
          Log.Log("Command code and data length are correct, att_channel=" + channel + ", att_type=" + type +
                  ", att_number=" + number);

          if (type != 50) {
            Log.Log("Att_type != 50, LORA controllers subsystem does not handling such (" + type +
                    ") att_type, return");
            return;
          }

          Log.Log("Searching intellectual modem object");
          foreach (var gatewayControllerInfo in _gatewayControllesManager.GatewayControllerInfos) {
            Log.Log("Checking object " + gatewayControllerInfo.Name);
            if (gatewayControllerInfo.Name == subObjectName) {
              Log.Log("Intellectual modem found, now searching for attached object information...");
              var attachedControllerName =
                _attachedControllersInfoSystem.GetAttachedControllerNameByConfig(subObjectName, channel, type, number);

              foreach (var loraControllerWithRxTopic in _loraControllersByRxTopicName) {
                var loraController = loraControllerWithRxTopic.Value;
                Log.Log("Checking LORA controller with name " + loraController.Name);
                if (attachedControllerName == loraController.Name) {
                  Log.Log("LORA controller was found, asking it for data...");
                  isLoraControllerFound =
                    true; // if found, code below must guarantee that notifyOperationComplete() would be called
                  loraController.GetDataInCallback(commandCode, data, (exception, bytes) => {
                    try {
                      if (exception == null) {
                        Log.Log("Данные от объекта LORA получены: " + bytes.ToText()); // TODO remove double enum
                        sendReplyAction((byte) (commandCode + 10), bytes.ToArray());
                        Log.Log("Данные от объекта LORA были отправлены в шлюз");
                        return;
                      }

                      Log.Log("Ошибка при запросе к LORA контроллеру: " + exception);
                    }
                    catch (Exception ex) {
                      Log.Log("При обработке ответа от объекта LORA возникло исключение: " + ex);
                    }
                    finally {
                      notifyOperationComplete(); // выполняется в другом потоке
                    }
                  });
                  break; // Далее связный с подключаемым объектом контроллер LORA искать не нужно
                }
              }

              break; // далее шлюз искать не нужно
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