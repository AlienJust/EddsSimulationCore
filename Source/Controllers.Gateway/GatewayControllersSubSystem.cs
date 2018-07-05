using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using AJ.Std.Composition;
using AJ.Std.Composition.Contracts;
using AJ.Std.Loggers;
using AJ.Std.Loggers.Contracts;
using AJ.Std.Text;
using AJ.Std.Text.Contracts;
using Audience;
using Controllers.Contracts;
using PollServiceProxy.Contracts;

namespace Controllers.Gateway {
  [Export(typeof(ICompositionPart))]
  public class GatewayControllersSubSystem : CompositionPartBase, ISubSystem, IGatewayControllerInfosSystem {
    private static readonly ILogger Log = new RelayMultiLogger(true,
      new RelayLogger(Env.GlobalLog,
        new ChainedFormatter(new ITextFormatter[]
          {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})),
      new RelayLogger(new ColoredConsoleLogger(ConsoleColor.DarkGreen, Console.BackgroundColor),
        new ChainedFormatter(new ITextFormatter[]
          {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})));

    private readonly List<IController> _controllers;
    private ICompositionRoot _compositionRoot;
    private readonly IEnumerable<IGatewayControllerInfo> _gatewayControllerInfos;
    private ICompositionPart _scadaPollGatewayPart;
    private IPollGateway _scadaPollGateway;

    public string SystemName => "Система шлюз-контроллеров";
    public override string Name => "GatewayControllers";

    public GatewayControllersSubSystem() {
      _controllers = new List<IController>();
      _gatewayControllerInfos =
        GatewayesXmlFactory.GetGatewaysConfigFromXml(Path.Combine(Env.CfgPath, "GatewayControllerInfos.xml"));
    }

    public void ReceiveData(string uplinkName, string subObjectName, byte commandCode, byte[] data,
      Action notifyOperationComplete, Action<int, IEnumerable<byte>> sendReplyAction) {
      try {
        Log.Log("Получены данные для контроллера " + subObjectName + ": код команды=" + commandCode +
                ", байты данных: " + data.ToText());
        var controller = _controllers.FirstOrDefault(c => c.Name == subObjectName);
        if (controller != null) {
          try {
            // Контроллер должен всегда! делать обратный вызов
            controller.GetDataInCallback(commandCode, data, (exception, bytes) => {
              try {
                if (exception != null) {
                  Log.Log("Контроллер " + subObjectName + " создал исключение: " + exception);
                  return;
                }

                var dataToSend = bytes.ToArray();
                Log.Log("Будут отправлены следующие данные от объекта " + subObjectName + ": " + dataToSend.ToText());
                sendReplyAction((byte) (commandCode + 10), dataToSend);
                //_gateway.SendData(uplinkName, subObjectName, (byte) (commandCode + 10), dataToSend);
                Log.Log("Данные были отправлены от лица контроллера " + subObjectName + ", байты данных: " +
                        dataToSend.ToText());
              }
              catch (Exception ex) {
                Log.Log("При обработке ответа от контроллера возникло исключение: " + ex);
              }
              finally {
                notifyOperationComplete();
              }
            });
          }
          catch (Exception ex) {
            Log.Log("Странно, ошибка во время запуска асинхронной операции, Вы уверены, что она асинхронная? " + ex);
          }
        }
        else {
          Log.Log("Не удалось найти контроллер под названием " + subObjectName);
        }
      }
      catch (Exception ex) {
        Log.Log("Ошибка при получении данных, исключение: " + ex);
        notifyOperationComplete();
      }
    }

    public override void SetCompositionRoot(ICompositionRoot root) {
      _compositionRoot = root;

      

      foreach (var gatewayControllerInfo in _gatewayControllerInfos) {
        _controllers.Add(new GateController(gatewayControllerInfo.Name));
      }

      Log.Log("SetCompositionRoot завершен, подсистема БУМИЗ была найдена, контроллеры подсистемы: ");
      foreach (var controller in _controllers) {
        Log.Log(controller.Name);
      }
      
      _scadaPollGatewayPart = _compositionRoot.GetPartByName("PollGateWay");
      _scadaPollGateway = _scadaPollGatewayPart as IPollGateway;
      if (_scadaPollGateway == null) throw new Exception("Не удалось найти PollGateWay через composition root");
      _scadaPollGatewayPart.AddRef();
      _scadaPollGateway.RegisterSubSystem(this);
    }

    public IEnumerable<IGatewayControllerInfo> GatewayControllerInfos => _gatewayControllerInfos;

    public override void BecameUnused() {
      _scadaPollGatewayPart.Release();
    }
  }
}