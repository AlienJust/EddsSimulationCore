using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using AJ.Std.Composition;
using AJ.Std.Composition.Contracts;
using AJ.Std.Concurrent;
using AJ.Std.Loggers;
using AJ.Std.Loggers.Contracts;
using AJ.Std.Text;
using AJ.Std.Text.Contracts;
using AJ.Std.Time;
using Audience;
using BumizIoManager.Contracts;
using BumizNetwork.Contracts;
using Commands.Bumiz.Intelecon;

namespace Bumiz.Apply.PulseCounterArchiveReader {
	public sealed class CountersSubSystem : CompositionPartBase, IPulseCounterDataStorageHolder {
		private static readonly ILogger Log = new RelayMultiLogger(true, new RelayLogger(Env.GlobalLog, new ChainedFormatter(new ITextFormatter[] {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})), new RelayLogger(new ColoredConsoleLogger(ConsoleColor.Green, Console.BackgroundColor), new ChainedFormatter(new ITextFormatter[] {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})));

		private ICompositionPart _bumizIoManagerPart;
		private IBumizIoManager _bumizIoManager;

		private readonly IPulseCounterDataStorage _storage;
		private readonly Thread _bumizArchivePollThread;
		private readonly Dictionary<string, IPulseCounterInfo> _counterInfos;
		private readonly List<IPulseCounterInfo> _availableInfos;
		private ICompositionRoot _compositionRoot;

		public CountersSubSystem() {
			_availableInfos = new List<IPulseCounterInfo>();
			_counterInfos = XmlFactory.GetCountersFromXml(Path.Combine(Env.CfgPath, "Bumiz.PulseCounters.xml"));
			_storage = new ConcurentPulseCounterDataStorage(new FilePulseCounterDataStorage(_counterInfos.Select(kvp => kvp.Value).ToList()));

			_bumizArchivePollThread = new Thread(ReadArchivesDataFromControllers);
		}

		private void ReadArchivesDataFromControllers() {
			var waitCounter = new WaitableCounter();
			var iterationPause = TimeSpan.FromMilliseconds(500);
			while (true) {
				try {
					foreach (var info in _availableInfos) {
						var nowTime = DateTime.Now.RoundToLatestHalfAnHour();
						var objName = info.Name;

						Log.Log("Работа с объектом " + objName + ", текущее время = " + nowTime.ToSimpleString());
						AsyncRecurseArchiveReadMethod(objName, waitCounter);
					}

					waitCounter.WaitForCounterChangeWhileNotPredecate(c => c == 0);
					Log.Log("Все команды были выполнены, пауза " + iterationPause.TotalSeconds.ToString("f2") + " секунд...");
					Thread.Sleep((int) iterationPause.TotalMilliseconds);
				}
				catch (Exception ex) {
					Log.Log(ex.ToString());
				}
			}
		}

		private void AsyncRecurseArchiveReadMethod(string objName, WaitableCounter sharedTasksCounter) {
			Log.Log("Рекурсивное чтение архивов для " + objName);
			var nowTime = DateTime.Now;
			DateTime? timeToGet = _storage.GetFirstMissedTimeUpToTime(objName, nowTime);
			if (timeToGet.HasValue) {
				var time = timeToGet.Value;
				var cmd = new ReadArchiveRecordServiceCommand(time);
				Log.Log("Есть архивы, которые нужно вычитать. Имя объекта=" + objName + "   Команда=<" + cmd.Comment + ">   Время=" + cmd.RequestedTime.ToSimpleString() + " Арх.№=" + cmd.RecordNumber);

				sharedTasksCounter.IncrementCount();
				_bumizIoManager.SendDataAsync(objName, cmd, result => {
					try {
						Log.Log("Асинхронный запрос к сети БУМИЗ выполнен для объекта " + objName);
						if (result != null) {
							if (result.ChannelException == null) {
								Log.Log(result.Bytes.ToText() + " <= для времени = " + time.ToSimpleString() + " Арх.№=" + cmd.RecordNumber);
								try {
									var ctResult = cmd.GetResult(result.Bytes);
									Log.Log(ctResult.ToString());
									if (ctResult.RecordTime.Date == time.Date) {
										Log.Log("Даты совпадают, сохраняем данные в хранилище");
										_storage.SaveData(objName, time, true, ctResult.Count1, ctResult.Count2, ctResult.Count3, ctResult.Status, ctResult.Xstatus);
									}
									else {
										throw new Exception("Дата внутри архива не совпадает с датой запроса");
									}
								}
								catch (Exception ex) {
									Log.Log("Ошибка обработки ответа БУМИЗ, в хранилище будет записана информация о плохой записи архива");
									Log.Log("Причина - исключение: " + ex.ToString());
									_storage.SaveData(objName, time, false, 0, 0, 0, 0, 0);
								}
								finally {
									AsyncRecurseArchiveReadMethod(objName, sharedTasksCounter);
								}
							}
							else {
								Log.Log("Ошибка канала передачи данных: " + result.ChannelException.ToString());
								Log.Log("Объект " + objName + " больше не будет опрашиваться в этой итерации (пока все остальные не закончат свои обмены)");
								// Получается, что при ошибке передачи данных следующий запрос не будет осуществлен, и объект выпадает из цикла опроса, пока другие объекты не закончат свои работы
							}
						}
						else {
							Log.Log("Результат выполнения операции не существует, странно :О");
						}
					}
					catch (Exception ex) {
						Log.Log("Произошла ошибка при разборе ответа от объекта " + objName);
						Log.Log(ex.ToString());
						// TODO: что делать при ошибке чтения данных (нет связи с FRAM)?
					}
					finally {
						Log.Log("Декремент счетчика задач сети БУМИЗ для объекта " + objName);
						sharedTasksCounter.DecrementCount();
					}
				}, IoPriority.Low);
			}
			else {
				Log.Log("Либо ошибка хранилища, либо для объекта " + objName + " все данные вычитаны");
			}
		}

		public override string Name => "BumizEvenSubSystem.PulseCounter";

		public override void SetCompositionRoot(ICompositionRoot root) {
			_compositionRoot = root;

			_bumizIoManagerPart = _compositionRoot.GetPartByName("BumizIoSubSystem");
			_bumizIoManager = _bumizIoManagerPart as IBumizIoManager;
			if (_bumizIoManager == null) throw new Exception("Не удалось найти BumizIoSubSystem через composition root");
			_bumizIoManagerPart.AddRef();

			foreach (var pulseCounterInfo in _counterInfos) {
				try {
					if (_bumizIoManager.BumizObjectExist(pulseCounterInfo.Key)) {
						_availableInfos.Add(pulseCounterInfo.Value);
					}
					else {
						Log.Log("Не удалось найти информацию о связи по каналу БУМИЗ с объектом " + pulseCounterInfo.Key);
					}
				}
				catch (Exception ex) {
					Log.Log("Не удалось связать информацию по импульсному счётчику " + pulseCounterInfo.Key + " с информацией о его сетевом расположении внутри сети БУМИЗ по причине:" + ex);
				}
			}

			// Поток обмена активируется при подключении родительской системы
			if (_counterInfos.Count > 0) {
				if (_bumizArchivePollThread.ThreadState == ThreadState.Unstarted)
					_bumizArchivePollThread.Start();
				else Log.Log("Странно, поток подсистемы чтения архивных данных уже был запущен!");
			}
			else {
				Log.Log("Подсистема не будет запущена, т.к. число контроллеров в конфигурации = 0");
			}
		}

		public IPulseCounterDataStorage Storage => _storage;

		public override void BecameUnused() {
			_bumizIoManagerPart.Release();
		}
	}
}