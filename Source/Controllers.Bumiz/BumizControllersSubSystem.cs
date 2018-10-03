using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AJ.Std.Composition;
using AJ.Std.Composition.Contracts;
using AJ.Std.Loggers;
using AJ.Std.Loggers.Contracts;
using AJ.Std.Text;
using AJ.Std.Text.Contracts;
using Audience;
using Bumiz.Apply.PulseCounterArchiveReader;
using BumizIoManager.Contracts;
using Controllers.Contracts;
using Controllers.Gateway;
using Controllers.Gateway.Attached;
using PollServiceProxy.Contracts;

namespace Controllers.Bumiz {
	public class BumizControllersSubSystem : CompositionPartBase, ISubSystem {
		private static readonly ILogger Log = new RelayMultiLogger(true, new RelayLogger(Env.GlobalLog, new ChainedFormatter(new ITextFormatter[] {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})), new RelayLogger(new ColoredConsoleLogger(ConsoleColor.DarkCyan, Console.BackgroundColor), new ChainedFormatter(new ITextFormatter[] {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})));

		private ICompositionPart _bumizIoManagerPart;
		private IBumizIoManager _bumizIoManager;

		private ICompositionPart _scadaPollGatewayPart;
		private IPollGateway _scadaPollGateway;

		private ICompositionPart _pulseCountersDataStoragePart;
		private IPulseCounterDataStorageHolder _pulseCountersDataStorage;

		private ICompositionPart _attachedControllersInfoSystemPart;
		private IAttachedControllersInfoSystem _attachedControllersInfoSystem;

		private ICompositionPart _gatewayControllesManagerPart;
		private IGatewayControllerInfosSystem _gatewayControllesManager;


		private ICompositionRoot _compositionRoot;
		private readonly List<IBumizControllerInfo> _bumizControllerInfos;
		private readonly List<IController> _bumizControllers;


		public string SystemName => "BumizControllers";

		public BumizControllersSubSystem() {
			_bumizControllerInfos = XmlFactory.GetBumizObjectInfosFromXml(Path.Combine(Env.CfgPath, "BumizControllerInfos.xml"));
			_bumizControllers = new List<IController>();
		}

		public void ReceiveData(string uplinkName, string subObjectName, byte commandCode, IReadOnlyList<byte> data, Action notifyOperationComplete, Action<int, IReadOnlyList<byte>> sendReplyAction) {
			bool isBumizControllerFound = false; // Если найден, то контроллер должен гарантировать выполнение вызова notifyOperationComplete
			try {
				Log.Log("Поступили данные от шлюза для объекта " + subObjectName + ", код команды = " + commandCode + ", данные: " + data.ToText());
				if (commandCode == 6 && data.Count >= 8) {
					var channel = data[0];
					var type = data[1];
					var number = data[2];
					Log.Log("Код команды и длина данных позволяют работать дальше, канал=" + channel + ", тип=" + type + ", номер=" + number);

					if (type != 13) {
						Log.Log("Тип счетчика не равен 13, обработка такой команды подсистемой БУМИЗ контроллеров не осуществляется");
						return;
					}

					Log.Log("Поиск объекта-шлюза...");
					foreach (var gatewayControllerInfo in _gatewayControllesManager.GatewayControllerInfos) {
						Log.Log("Проверка объекта " + gatewayControllerInfo.Name);
						if (gatewayControllerInfo.Name == subObjectName) {
							Log.Log("Объект-шлюз найден, поиск подключенного объекта...");
							var attachedControllerName = _attachedControllersInfoSystem.GetAttachedControllerNameByConfig(subObjectName, channel, type, number);
							Log.Log("Подключаемый объект найден, поиск соответствующего объекта БУМИЗ...");

							foreach (var bumizController in _bumizControllers) {
								Log.Log("Проверка объекта БУМИЗ " + bumizController.Name);
								if (bumizController.Name == attachedControllerName) {
									Log.Log("Объект БУМИЗ найден, запрос данных от объекта...");
									isBumizControllerFound = true;
									//IGatewayControllerInfo info = gatewayControllerInfo; // для замыкания
									bumizController.GetDataInCallback(commandCode, data, (exception, bytes) => {
										try {
											if (exception == null) {
												Log.Log("Данные от объекта БУМИЗ получены: " + bytes.ToText()); // TODO remove double enum
												sendReplyAction((byte) (commandCode + 10), bytes.ToArray());
												Log.Log("Данные от объекта БУМИЗ были отправлены в шлюз");
												return;
											}

											Log.Log("Ошибка при запросе к БУМИЗ контроллеру: " + exception);
										}
										catch (Exception ex) {
											Log.Log("При обработке ответа от объекта БУМИЗ возникло исключение: " + ex);
										}
										finally {
											notifyOperationComplete(); // выполняется в другом потоке
										}
									});
									break; // Далее связный с подключаемым объектом контроллер БУМИЗ искать не нужно
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
				if (!isBumizControllerFound) {
					notifyOperationComplete(); // cause I need to ensure callback is called
				}
			}
		}

		public override void SetCompositionRoot(ICompositionRoot root) {
			_compositionRoot = root;

			_scadaPollGatewayPart = _compositionRoot.GetPartByName("PollGateWay");
			_scadaPollGateway = _scadaPollGatewayPart as IPollGateway;
			if (_scadaPollGateway == null) throw new Exception("Не удалось найти PollGateWay через composition root");
			_scadaPollGatewayPart.AddRef();

			_bumizIoManagerPart = _compositionRoot.GetPartByName("BumizIoSubSystem");
			_bumizIoManager = _bumizIoManagerPart as IBumizIoManager;
			if (_bumizIoManager == null) throw new Exception("Не удалось найти BumizIoSubSystem через composition root");
			_bumizIoManagerPart.AddRef();

			_pulseCountersDataStoragePart = _compositionRoot.GetPartByName("BumizEvenSubSystem.PulseCounter");
			_pulseCountersDataStorage = _pulseCountersDataStoragePart as IPulseCounterDataStorageHolder;
			if (_pulseCountersDataStorage == null)
				throw new Exception("Не удалось найти держатель хранилища импульсных счетчиков через composition root");
			_pulseCountersDataStoragePart.AddRef();

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

			foreach (var bumizControllerInfo in _bumizControllerInfos) {
				if (_bumizIoManager.BumizObjectExist(bumizControllerInfo.Name)) {
					_bumizControllers.Add(new BumizController(_bumizIoManager, _pulseCountersDataStorage, bumizControllerInfo));
				}
				else {
					Log.Log("Не удалось найти информацию о связи по сети БУМИЗ для контроллера: " + bumizControllerInfo.Name);
				}
			}

			Log.Log("Подсистема подключаемых контроллеров БУМИЗ инициализирована, число контроллеров: " + _bumizControllers.Count);
		}

		public override string Name => "BumizControllers";

		public override void BecameUnused() {
			_scadaPollGatewayPart.Release();
			_bumizIoManagerPart.Release();
			_pulseCountersDataStoragePart.Release();
			_attachedControllersInfoSystemPart.Release();
			_gatewayControllesManagerPart.Release();
		}
	}
}