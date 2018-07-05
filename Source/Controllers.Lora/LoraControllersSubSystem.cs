using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using AJ.Std.Composition;
using AJ.Std.Composition.Contracts;
using AJ.Std.Loggers;
using AJ.Std.Loggers.Contracts;
using AJ.Std.Text;
using AJ.Std.Text.Contracts;
using Audience;
using Controllers.Contracts;
using Controllers.Gateway;
using Controllers.Gateway.Attached;
using PollServiceProxy.Contracts;

namespace Controllers.Lora {
  [Export(typeof(ICompositionPart))]
  public class LoraControllersSubSystem : CompositionPartBase, ISubSystem {
    private static readonly ILogger Log = new RelayMultiLogger(true,
      new RelayLogger(Env.GlobalLog,
        new ChainedFormatter(new ITextFormatter[]
          {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})),
      new RelayLogger(new ColoredConsoleLogger(ConsoleColor.Black, ConsoleColor.Yellow),
        new ChainedFormatter(new ITextFormatter[]
          {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})));


    private ICompositionPart _scadaPollGatewayPart;
    private IPollGateway _scadaPollGateway;


    private ICompositionPart _attachedControllersInfoSystemPart;
    private IAttachedControllersInfoSystem _attachedControllersInfoSystem;

    private ICompositionPart _gatewayControllesManagerPart;
    private IGatewayControllerInfosSystem _gatewayControllesManager;


    private ICompositionRoot _compositionRoot;
    private readonly IEnumerable<LoraControllerInfoSimple> _loraControllerInfos;
    private readonly List<IController> _loraControllers;


    public string SystemName => "LoraControllers";

    private readonly string _mqttTopicStart;

    public LoraControllersSubSystem() {
      _mqttTopicStart = "application/1/node/";
      _loraControllerInfos =
        new List<LoraControllerInfoSimple> {new LoraControllerInfoSimple("lora1", "be7a0000000000c8")};
      _loraControllers = new List<IController>();
    }


    public override void SetCompositionRoot(ICompositionRoot root) {
      _compositionRoot = root;

      _scadaPollGatewayPart = _compositionRoot.GetPartByName("PollGateWay");
      _scadaPollGateway = _scadaPollGatewayPart as IPollGateway;
      if (_scadaPollGateway == null)
        throw new Exception("Не удалось найти PollGateWay через composition root");
      _scadaPollGatewayPart.AddRef();


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

      foreach (var loraControllerInfo in _loraControllerInfos) {
        _loraControllers.Add(new LoraController(loraControllerInfo.Name, loraControllerInfo.DeviceId, _mqttTopicStart,
          Log.Log));
      }

      Log.Log("Подсистема подключаемых контроллеров БУМИЗ инициализирована, число контроллеров: " +
              _loraControllers.Count);
    }


    public void ReceiveData(string uplinkName, string subObjectName, byte commandCode, byte[] data,
      Action notifyOperationComplete, Action<int, IEnumerable<byte>> sendReplyAction) {
      bool isLoraControllerFound =
        false; // Если найден, то контроллер должен гарантировать выполнение вызова notifyOperationComplete
      try {
        Log.Log("Поступили данные от шлюза для объекта " + subObjectName + ", код команды = " +
                commandCode + ", данные: " + data.ToText());
        if (commandCode == 6 && data.Length >= 8) {
          var channel = data[0];
          var type = data[1];
          var number = data[2];
          Log.Log("Код команды и длина данных позволяют работать дальше, канал=" + channel + ", тип=" +
                  type + ", номер=" + number);

          if (type != 13) {
            Log.Log(
              "Тип счетчика не равен 13, обработка такой команды подсистемой LORA контроллеров не осуществляется");
            return;
          }

          Log.Log("Поиск объекта-шлюза...");
          foreach (var gatewayControllerInfo in _gatewayControllesManager.GatewayControllerInfos) {
            Log.Log("Проверка объекта " + gatewayControllerInfo.Name);
            if (gatewayControllerInfo.Name == subObjectName) {
              Log.Log("Объект-шлюз найден, поиск подключенного объекта...");
              foreach (var attachedControllerInfo in _attachedControllersInfoSystem
                .AttachedControllerInfos) {
                Log.Log("Проверка подключаемого объекта " + attachedControllerInfo.Name);
                if (attachedControllerInfo.Channel == channel &&
                    attachedControllerInfo.Type == type && attachedControllerInfo.Number == number) {
                  Log.Log("Подключаемый объект найден, поиск соответствующего объекта LORA...");
                  var loraObjName = attachedControllerInfo.Name;

                  foreach (var loraController in _loraControllers) {
                    Log.Log("Проверка объекта LORA " + loraController.Name);
                    if (loraObjName == loraController.Name) {
                      Log.Log("Объект LORA найден, запрос данных от объекта...");
                      isLoraControllerFound = true;
                      //IGatewayControllerInfo info = gatewayControllerInfo; // для замыкания
                      loraController.GetDataInCallback(
                        commandCode,
                        data,
                        (exception, bytes) => {
                          try {
                            if (exception == null) {
                              Log.Log("Данные от объекта LORA получены: " +
                                      bytes.ToText()); // TODO remove double enum
                              sendReplyAction((byte) (commandCode + 10),
                                bytes.ToArray());
                              Log.Log(
                                "Данные от объекта LORA были отправлены в шлюз");
                              return;
                            }

                            Log.Log("Ошибка при запросе к LORA контроллеру: " +
                                    exception);
                          }
                          catch (Exception ex) {
                            Log.Log(
                              "При обработке ответа от объекта LORA возникло исключение: " +
                              ex);
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

    public override string Name => "BumizControllers";

    public override void BecameUnused() {
      _scadaPollGatewayPart.Release();
      _attachedControllersInfoSystemPart.Release();
      _gatewayControllesManagerPart.Release();
    }
  }
}