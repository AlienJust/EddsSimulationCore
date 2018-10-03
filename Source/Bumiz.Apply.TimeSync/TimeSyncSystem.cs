using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using AJ.Std.Composition;
using AJ.Std.Composition.Contracts;
using AJ.Std.Loggers;
using AJ.Std.Loggers.Contracts;
using AJ.Std.Text;
using AJ.Std.Text.Contracts;
using Audience;
using BumizIoManager.Contracts;
using BumizNetwork.Contracts;
using Commands.Bumiz.CounterSe102;
using Commands.Bumiz.Intelecon;

namespace Bumiz.Apply.TimeSync {
	public class TimeSyncSystem : CompositionPartBase {
		private static readonly ILogger Log = new RelayMultiLogger(true, new RelayLogger(Env.GlobalLog, new ChainedFormatter(new ITextFormatter[] {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})), new RelayLogger(new ColoredConsoleLogger(ConsoleColor.Red, Console.BackgroundColor), new ChainedFormatter(new ITextFormatter[] {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})));

		private readonly IList<string> _objectsToSync;
		private readonly List<string> _bumizNames;
		private readonly Thread _bumizTimeSyncThread;

		private ICompositionPart _bumizIoManagerPart;
		private IBumizIoManager _bumizIoManager;

		private ICompositionRoot _compositionRoot;

		public TimeSyncSystem() {
			IObjectsInfoLoader infoLoader = new ObjectsInfoLoaderXml(Path.Combine(Env.CfgPath, "Bumiz.TimeSync.Objects.xml"));
			_objectsToSync = infoLoader.GetObjects();
			_bumizTimeSyncThread = new Thread(SyncTimeFunc);
			_bumizNames = new List<string>();
		}

		private void SyncTimeFunc() {
			var getTimeCmd = new GetCounterTimeCommand();
			var wrappedGetTimeCmd = new WrappedCounterCommand(getTimeCmd);

			var setTimeCmd = new SetCounterTimeToCurrentCommand(TimeSpan.FromSeconds(5.0));
			var wrappedSetTimeCmd = new WrappedCounterCommand(setTimeCmd);

			while (true) {
				var waiter = new AutoResetEvent(false);

				// TODO: данный алгоритм не поддерживает возможности параллельной работы по нескольким каналам одновременно (все объекты синхронизируются последовательно)
				// TODO: чтобы включить таковую поддержку, нужно создать threadWorker для отправки команд
				// TODO: но тогда теряется выполнение в реальном времени (то есть, грубо говоря, время будет устанавливаться не точно, если в очереди с высоким приоритетом 100500 команд)
				// TODO: т.к. DateTime для установки задается во время добавления команды в очередь, а не во время извлечения
				// TODO: решение: можно написать команду, которая будет подставлять то текущее время на момент распаковки! SetCounterTimeCurrentCommand - написал SetCounterTimeToCurrentCommand
				foreach (var bumizName in _bumizNames) {
					var objectName = bumizName;
					//var obj = info.Item1;
					//var channel = info.Item2;

					bool canSyncTime = false;

					_bumizIoManager.SendDataAsync(objectName,
						//channel.SendInteleconCommandAsync(
						wrappedGetTimeCmd, result => {
							try {
								if (result.ChannelException == null) {
									var counterReply = result.Bytes.GetDataBytesFromCounterReply();
									Log.Log("Получены байты:" + counterReply.ToText());
									var curControllerTime = getTimeCmd.GetResult(counterReply);
									var currentTime = DateTime.Now;
									canSyncTime = CanSafeAndReallyNeedTimeSync(curControllerTime, currentTime);
									Log.Log("Время контроллера " + objectName + ": " + curControllerTime.ToString("yyyy.MM.dd-HH:mm:ss") + (canSyncTime ? " будет произведена синхронизация" : " синхронизация не требуется или невозможна в автоматическом режиме"));
								}
								else {
									throw result.ChannelException;
								}
							}
							catch (Exception ex) {
								Log.Log("Ошибка при обработке ответа команды получения времени контроллера: " + ex);
							}
							finally {
								waiter.Set(); // в любом случае нужно продолжить алгоритм
							}
						}, IoPriority.Lowest); // тут высокий приоритет не нужен, главное, чтобы команда установки времени выполнилась быстро (чтобы успеть во временное окно)

					Log.Log("Ждем результатов чтения времени объекта " + objectName + " ...");
					waiter.WaitOne();

					if (canSyncTime) {
						_bumizIoManager.SendDataAsync(objectName,
							//channel.SendInteleconCommandAsync(
							wrappedSetTimeCmd, result => {
								try {
									if (result.ChannelException == null) {
										Log.Log("Синхронизация прошла успешно для объекта " + objectName);
									}
									else {
										throw result.ChannelException;
									}
								}
								catch (Exception ex) {
									Log.Log("Ошибка при обработке ответа команды установки времени контроллера: " + ex);
								}
								finally {
									waiter.Set(); // finally waited ok
								}
							}, IoPriority.Highest);
						waiter.WaitOne();
					}

					Thread.Sleep(300000);
				}

				Thread.Sleep(10000);
			}
		}

		static bool CanSafeAndReallyNeedTimeSync(DateTime controllerTime, DateTime currentTime) {
			if (controllerTime == currentTime) return false;

			DateTime lastTime;
			DateTime firstTime;
			if (currentTime > controllerTime) {
				lastTime = currentTime;
				firstTime = controllerTime;
			}
			else {
				lastTime = controllerTime;
				firstTime = currentTime;
			}

			var span = lastTime - firstTime;
			Log.Log("Разница времен = " + span.TotalSeconds.ToString("f3") + " сек.");

			if (span.TotalMinutes > 2.0) {
				if ((lastTime.Minute > 2 && lastTime.Minute < 28 || lastTime.Minute > 32 && lastTime.Minute < 58) && (firstTime.Minute > 2 && firstTime.Minute < 28 || firstTime.Minute > 32 && firstTime.Minute < 58)) {
					return true;
				}
			}

			return false;
		}

		public override string Name => "BumizEvenSubSystem.TimeSync";

		public override void SetCompositionRoot(ICompositionRoot root) {
			_compositionRoot = root;

			_bumizIoManagerPart = _compositionRoot.GetPartByName("BumizIoSubSystem");
			_bumizIoManager = _bumizIoManagerPart as IBumizIoManager;
			if (_bumizIoManager == null) throw new Exception("Не удалось найти BumizIoSubSystem через composition root");
			_bumizIoManagerPart.AddRef();

			foreach (var objSyncInfo in _objectsToSync) {
				try {
					if (_bumizIoManager.BumizObjectExist(objSyncInfo)) {
						_bumizNames.Add(objSyncInfo);
					}
					else {
						Log.Log("Не удалось связать информацию по объекту " + objSyncInfo + " с информацией о его сетевом расположении внутри сети БУМИЗ, видимо конфигурация сетевого расположения отсутствует");
					}
				}
				catch (Exception ex) {
					Log.Log("Не удалось связать информацию по объекту " + objSyncInfo + " с информацией о его сетевом расположении внутри сети БУМИЗ по причине:" + ex);
				}
			}

			if (_bumizNames.Count > 0) {
				if (_bumizTimeSyncThread.ThreadState == ThreadState.Unstarted)
					_bumizTimeSyncThread.Start();
				else Log.Log("Странно, поток подсистемы синхронизации времени БУМИЗов уже запущен");
			}
			else {
				Log.Log("Подсистема не будет запущена, т.к. число объектов конфигурации = 0");
			}
		}

		public override void BecameUnused() {
			_bumizIoManagerPart.Release();
		}
	}
}