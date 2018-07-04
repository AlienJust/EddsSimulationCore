using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
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
using PollServiceProxy.Contracts;
using ScadaClient.Contracts;

namespace PollServiceProxy {
	[Export(typeof(ICompositionPart))]
	public sealed class PollGateway : CompositionPartBase, IPollGateway, ISubSystemRegistrationPoint {
		private static readonly ILogger Log = new RelayMultiLogger(true, new RelayLogger(Env.GlobalLog, new ChainedFormatter(new ITextFormatter[] { new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ") })), new RelayLogger(new ColoredConsoleLogger(ConsoleColor.Cyan, Console.BackgroundColor), new ChainedFormatter(new ITextFormatter[] { new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ") })));
		private readonly Dictionary<string, INamedScadaLink> _scadaClients;
		private readonly Dictionary<string, IScadaObjectInfo> _scadaObjects;
		private readonly List<ISubSystem> _internalSystems;
		private readonly Thread _microPacketSendThread;
		private readonly int _microPacketSendingIntervalMs;
		private ICompositionRoot _compositionRoot;
		private readonly Dictionary<IScadaAddress, IWorker<Action>> _perScadaAddressWorkers;

		public PollGateway() {

			_perScadaAddressWorkers = new Dictionary<IScadaAddress, IWorker<Action>>();

			_internalSystems = new List<ISubSystem>();

			_scadaClients = XmlFactory.GetScadaLinksFromXml(Path.Combine(Env.CfgPath, "Servers.xml"));
			_scadaObjects = XmlFactory.GetScadaObjectsFromXml(Path.Combine(Env.CfgPath, "ScadaObjects.xml"));
			_microPacketSendingIntervalMs = XmlFactory.GetMicroPacketSendingIntervalMsFromXml(Path.Combine(Env.CfgPath, "PollServiceProxy.xml"));

			_microPacketSendThread = new Thread(SendMicroPackets); // Инифиализация потока отправки микропакетов (но не запуск)
		}

		public override void SetCompositionRoot(ICompositionRoot root) {
			_compositionRoot = root;
			//_internalSystems.AddRange(_compositionRoot.Compositions.OfType<ISubSystem>());

			//foreach (var subSystem in _internalSystems) {
				//subSystem.SetGateway(this);
			//}

			//Log.Log("Загружены следующие подсистемы: (количество = " + _internalSystems.Count + ")");
			//foreach (var internalSystem in _internalSystems) {
				//Log.Log(internalSystem.SystemName);
			//}


			if (_microPacketSendThread.ThreadState == ThreadState.Unstarted) {
				foreach (var scadaObjectInfo in _scadaObjects) {
					foreach (var scadaAddress in scadaObjectInfo.Value.ScadaAddresses) {
						// На каждый объект скады с сетевым адерсом создается свой поток

						// TODO: strategy selection:
						// TODO: Add to XML configuration
						// TODO: strategy: if working then drop 
						// TODO:    or   : if working then enqueue
						_perScadaAddressWorkers.Add(scadaAddress, new WorkerSingleThreadedRelayDrop<Action>(a => a(), ThreadPriority.Normal, true, null));
						//_perScadaAddressWorkers.Add(scadaAddress, new SingleThreadedRelayQueueWorker<Action>(a => a(), ThreadPriority.Normal, true, null));
					}
				}

				// к каждому PollService цепляется событие о получении данных от него:
				foreach (var scadaClient in _scadaClients) {
					scadaClient.Value.DataReceived += OnScadaLinkDataReceived;
				}

				_microPacketSendThread.Start();
				Log.Log("СКАДА серверы и объекты инициализированы, поток отправки микропакетов запущен");
			}
			else {
				Log.Log("Странно, поток микропакетов уже запущен!");
			}
		}


		private void OnScadaLinkDataReceived(object sender, DataReceivedEventArgs eventArgs) {
			// Данные от скады получены - вызывается в хрен знает каком потоке.
			try {
				var scadaClient = sender as INamedScadaLink;
				if (scadaClient == null) {
					Log.Log("Не удалось преобразовать объект, создавший событие к интерфейсу INamedScadaLink, отмена обработки входных данных");
					return;
				}



				// для всех объектов, которые относятся к PollService приславшему данные и имеет нужный сетевой адрес
				var scadaObjectNetAddress = eventArgs.NetAddress;
				foreach (var scadaObjectInfo in _scadaObjects) {
					foreach (var objectScadaAddress in scadaObjectInfo.Value.ScadaAddresses) {
						if (objectScadaAddress.LinkName == scadaClient.Name && objectScadaAddress.NetAddress == eventArgs.NetAddress) {
							var scadaObjectName = scadaObjectInfo.Key;
							//var scadaObjectNetAddress = objectScadaAddress.NetAddress;

							Log.Log("Получен запрос для контроллера, имеющего адрес скады " + objectScadaAddress + ". Адрес найден в конфигурации, все подсистемы шлюза будут оповещены в обработчике запросов, привязанному к этому адресу скады");
							_perScadaAddressWorkers[objectScadaAddress].AddWork(
								() => {
									var counter = new WaitableCounter(0);
									foreach (var internalSystem in _internalSystems) {
										try {
											Log.Log("Оповещение подсистемы " + internalSystem.SystemName);
											counter.IncrementCount();

											internalSystem.ReceiveData(
												scadaClient.Name,
												scadaObjectName,
												eventArgs.CommandCode,
												eventArgs.Data,
												counter.DecrementCount,
												(code, reply) => SendReplyData(scadaClient.Name, scadaObjectNetAddress, (byte)code, reply.ToArray()));
										}
										catch (Exception ex) {
											Log.Log("Произошла ошибка при работе с одной из подсистем: " + ex);
											//counter.DecrementCount(); // TODO: do I really need this?
										}
									}
									Log.Log("Ожидание завершения работы всех подсистем");
									counter.WaitForCounterChangeWhileNotPredecate(c => c == 0);
									Log.Log("Операция по всем оповещенным подсистемам для данного адреса скады завершена");
								});
							break;
						}
					}
				}
			}
			catch (Exception ex) {
				Log.Log("Произошла ошибка при получении данных от одной из скад: " + ex);
			}
		}

		private void SendMicroPackets() {
			while (true) {
				int sendsCount = 0;
				foreach (var obj in _scadaObjects) {
					if (obj.Value.SendMicroPackets) {
						foreach (var target in obj.Value.ScadaAddresses) {
							try {
								_scadaClients[target.LinkName].Link.SendMicroPacket((ushort)target.NetAddress, 22);
								sendsCount++;
							}
							catch (Exception ex) {
								Log.Log("Не удалось отправить микропакет, причина:");
								Log.Log(ex.ToString());
							}
						}
					}
				}
				Log.Log("Было отправлено микропакетов: " + sendsCount);
				Thread.Sleep(_microPacketSendingIntervalMs);
			}
		}

		public void SendData(string uplinkName, string scadaObjectName, byte commandCode, byte[] data) {
			// TODO: make threadsafe
			var scadaObjectInfo = _scadaObjects[scadaObjectName];
			var scadaAddress = scadaObjectInfo.ScadaAddresses.First(t => t.LinkName == uplinkName);

			_perScadaAddressWorkers[scadaAddress].AddWork(() => SendReplyData(uplinkName, (ushort)scadaAddress.NetAddress, commandCode, data));
		}

		private void SendReplyData(string uplinkName, ushort netAddress, byte commandCode, byte[] data) {
			_scadaClients[uplinkName].Link.SendData(netAddress, commandCode, data);
		}

		public override string Name => "PollGateWay";

		public void RegisterSubSystem(ISubSystem subSystem)
		{
			_internalSystems.Add(subSystem);
			Log.Log("Зарегистрирована подсистема обработки входящих данных " + subSystem.SystemName);
		}

		public override void BecameUnused()
		{
			// release all compostionparts
		}
	}
}