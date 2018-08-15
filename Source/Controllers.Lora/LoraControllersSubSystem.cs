using System;
using System.Collections.Generic;
using System.Composition;
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
using Controllers.Contracts;
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

    public LoraControllersSubSystem() {
      _backWorker = new SingleThreadedRelayQueueWorkerProceedAllItemsBeforeStopNoLog<Action>("Lora (mqtt) background worker", a=>a(), ThreadPriority.BelowNormal, true, null);
      _prevSubscribeIsComplete = new AutoResetEvent(false);
      _initComplete = new ManualResetEvent(false);
      
      _loraControllersByRxTopicName = new Dictionary<string, LoraController>();
      
      _mqttClient = new MqttClient(_mqttBrokerHost, Guid.NewGuid().ToString());
      _mqttClient.Port = _mqttBrokerPort;

      _mqttClient.OnMessageReceived += OnMessageReceived;
      _mqttClient.ConnectAsync().Wait();
      
      _mqttTopicStart = "application/1/node/";
      _loraControllerInfos = new List<LoraControllerInfoSimple> {
        new LoraControllerInfoSimple("lora99", "be7a0000000000c8")
        , new LoraControllerInfoSimple("lora100", "be7a0000000000c9")
      };
      
      Log.Log("Waits until all RX topics would be subscribed...");
      _initComplete.WaitOne();
      Log.Log(".ctor complete");
    }

    private void OnMessageReceived(MqttMessage message) {
      switch (message) {
        case ConnAckMessage msg:
          Console.WriteLine("---- OnConnAck");
          Log.Log("MQTT connected OK");
          _backWorker.AddWork(() => {
            _loraControllersByRxTopicName.Clear();
            foreach (var loraControllerInfo in _loraControllerInfos) {
              var rxTopicName = _mqttTopicStart + loraControllerInfo.DeviceId + "/rx";
              var txTopicName = _mqttTopicStart + loraControllerInfo.DeviceId + "/tx";
              _loraControllersByRxTopicName.Add(rxTopicName, new LoraController(loraControllerInfo.Name, txTopicName, Log.Log, _mqttClient));
              Log.Log("Subscribing for topic: " + rxTopicName);
              Log.Log("Waiting for SubscribeAckMessage from MQTT broker...");
              _mqttClient.Subscribe(rxTopicName, Qos.AtLeastOnce);
              //_prevSubscribeIsComplete.WaitOne(TimeSpan.FromSeconds(5)); // something wrong if cannot get SubAck
              _prevSubscribeIsComplete.WaitOne(TimeSpan.FromMinutes(1.0));
              Log.Log("Subscribed for topic" + rxTopicName + " OK");
            }

            _initComplete.Set();
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
            _loraControllersByRxTopicName[msg.TopicName].OnMessageReceived(msg);
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


    public void ReceiveData(string uplinkName, string subObjectName, byte commandCode, byte[] data
      , Action notifyOperationComplete, Action<int, IEnumerable<byte>> sendReplyAction) {
      bool isLoraControllerFound =
        false; // Если найден, то контроллер должен гарантировать выполнение вызова notifyOperationComplete
      try {
        Log.Log("Поступили данные от шлюза для объекта " + subObjectName + ", код команды = " + commandCode +
                ", данные: " + data.ToText());
        if (commandCode == 6 && data.Length >= 8) {
          var channel = data[0];
          var type = data[1];
          var number = data[2];
          Log.Log("Код команды и длина данных позволяют работать дальше, канал=" + channel + ", тип=" + type +
                  ", номер=" + number);

          if (type != 50) {
            Log.Log(
              "Тип счетчика не равен 50, обработка такой команды подсистемой LORA контроллеров не осуществляется");
            return;
          }

          Log.Log("Поиск объекта-шлюза...");
          foreach (var gatewayControllerInfo in _gatewayControllesManager.GatewayControllerInfos) {
            Log.Log("Проверка объекта " + gatewayControllerInfo.Name);
            if (gatewayControllerInfo.Name == subObjectName) {
              Log.Log("Объект-шлюз найден, поиск подключенного объекта...");
              foreach (var attachedControllerInfo in _attachedControllersInfoSystem.AttachedControllerInfos) {
                Log.Log("Проверка подключаемого объекта " + attachedControllerInfo.Name + " ch=" +
                        attachedControllerInfo.Channel + ", number=" + attachedControllerInfo.Number + ", type=" +
                        attachedControllerInfo.Type);
                if (attachedControllerInfo.Channel == channel && attachedControllerInfo.Type == type &&
                    attachedControllerInfo.Number == number) {
                  Log.Log("Подключаемый объект найден, поиск соответствующего объекта LORA...");
                  var loraObjName = attachedControllerInfo.Name;

                  foreach (var loraControllerWithRxTopic in _loraControllersByRxTopicName) {
                    var loraController = loraControllerWithRxTopic.Value;
                    Log.Log("Проверка объекта LORA " + loraController.Name);
                    if (loraObjName == loraController.Name) {
                      Log.Log("Объект LORA найден, запрос данных от объекта...");
                      isLoraControllerFound = true;

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
                      break; // Далее связный с подключаемым объектом контроллер БУМИЗ искать не нужно
                    }
                  }

                  break; // Далее подключаемый к шлюзу объект искать не нужно
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