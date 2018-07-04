using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AJ.Std.Concurrent;
using AJ.Std.Concurrent.Contracts;
using AJ.Std.Loggers;
using AJ.Std.Loggers.Contracts;
using AJ.Std.Text;
using AJ.Std.Text.Contracts;
using Audience;
using BumizNetwork.Contracts;
using BumizNetwork.RawQueuing;
using BumizNetwork.RawQueuing.Contracts;
using BumizNetwork.SerialChannel;
using BumizNetwork.Shared;

namespace BumizNetwork {
	/// <summary>
	/// Это по сути моно-канал (базируется на COM-порте)
	/// </summary>
	public class BumizAdvancedNetwork : IMonoChannel {
		private static readonly ILogger Log = new RelayMultiLogger(true, new RelayLogger(Env.GlobalLog, new ChainedFormatter(new ITextFormatter[] { new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ") })), new RelayLogger(new ColoredConsoleLogger(ConsoleColor.Yellow, Console.BackgroundColor), new ChainedFormatter(new ITextFormatter[] { new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ") })));
		private readonly object _sync = new object();

		private readonly SerialChannelSimple _channelSimple; // Канал
		private readonly IDNetIdsStorage _addressMap;
		private readonly ILinkQualityStorage _linkQualityStorage;

		private bool _isTaskExecuting;
		private readonly IMultiQueueWorker<Action> _queueWorker;
		private readonly IWorker<Action> _notifyWorker;
		private int _queueLength;

		private Action _queueChangedCallback;
		private readonly TimeSpan _onlineCheckTime;
		private readonly bool _checkInteleconAddress;

		public BumizAdvancedNetwork(SerialChannelSimple channelSimple, Action queueChangedCallback, int onlineCheckTimeSeconds, bool checkInteleconAddress) {
			Log.Log("Создание канала сети БУМИЗ поверх COM-порта...");

			_addressMap = new DNetIdsFileDataStorage("DNetIdStorage");
			_linkQualityStorage = new LinkQualityStorage();

			_channelSimple = channelSimple ?? throw new Exception("SerialChannelSimple cannot be null");
			_onlineCheckTime = TimeSpan.FromSeconds(onlineCheckTimeSeconds);

			QueueChangedCallback = queueChangedCallback;
			_checkInteleconAddress = checkInteleconAddress;
			_queueLength = 0;


			_queueWorker = new SingleThreadedRelayMultiQueueWorker<Action>("BumizAdvancedNetwork.QueueWorker", a => a(), ThreadPriority.Normal, true, null, null, 5); // 5 levels of IO priority
			_notifyWorker = new SingleThreadedRelayQueueWorkerProceedAllItemsBeforeStopNoLog<Action>("BumizAdvancedNetwork.NotifyWorker", a => a(), ThreadPriority.Normal, true, null);

			Log.Log("Создание сети завершено");
		}

		public void AddCommandToQueueAndExecuteAsync(object item) {
			AddCommandToQueueAndExecuteAsync(item, IoPriority.Normal);
		}

		public void AddCommandToQueueAndExecuteAsync(object item, IoPriority priority) {
			_queueWorker.AddWork(() => {
				try {
					QueueLength++;
					NotifyQueueCountChanged();
					IsTaskExecuting = true;
					if (item is QueueItem queueItem) {
						var addressItem = queueItem;
						var progressItemPart = new List<ISendResult>();
						foreach (var sendItem in addressItem.SendingItems) {
							Exception exc = null;
							byte[] result = null;
							try {
								var lastBadTime = _linkQualityStorage.GetLastBadTime(sendItem.Address);
								var nowTime = DateTime.Now;

								if (lastBadTime.HasValue && nowTime - lastBadTime.Value < _onlineCheckTime) {
									exc = new Exception(lastBadTime.Value.ToString("yyyy.MM.dd-HH:mm:ss") + " с объектом по адресу " + sendItem.Address + " не получалось установить связь, так что еще рано с ним связываться (прошло времени: " + (nowTime - lastBadTime.Value).TotalSeconds.ToString("f2") + " сек, а должно пройти: " + _onlineCheckTime.TotalSeconds.ToString("f2") + " сек)");
								}
								else {
									Log.Log("Запрос в сеть БУМИЗ для объекта по адресу" + sendItem.Address);
									result = AskForData(sendItem.Address, sendItem.Buffer, sendItem.AttemptsCount, sendItem.WaitTimeout);
									_linkQualityStorage.SetLastBadTime(sendItem.Address, null);
									Log.Log("Связь с объектом по адресу " + sendItem.Address + " была установлена, время последнего неудачного обмена обнулено");
								}
							}
							catch (Exception ex) {
								var lastBadTime = DateTime.Now;
								_linkQualityStorage.SetLastBadTime(sendItem.Address, lastBadTime);
								exc = ex;
								Log.Log("Во время обмена произошла ошибка, поэтому время неудачного обмена установлено в значение: " + lastBadTime.ToString("yyyy.MM.dd-HH:mm:ss") + ", исключение: " + ex);
							}
							finally {
								progressItemPart.Add(new SendingResult(result, exc, sendItem));
							}
						}
						_notifyWorker.AddWork(() => addressItem.OnComplete(progressItemPart));
					}
					else if (item is IQueueRawItem) {
						Log.Log("Отправка произвольных данных в порт...");
						var rawItem = item as IQueueRawItem;
						var result = new SendRawResultSimple();
						try {
							result.Bytes = _channelSimple.RequestBytes(rawItem.SendItem.SendingBytes.ToArray(), rawItem.SendItem.AwaitedBytesCount, 10);
						}
						catch (Exception ex) {
							result.ChannelException = ex;
						}

						if (rawItem.OnSendComplete != null) {
							_notifyWorker.AddWork(() => rawItem.OnSendComplete(result));
						}
					}
				}
				catch (Exception ex) {
					Log.Log("В задании для обработчика очереди обмена произошло исключение: " + ex);
				}
				finally {
					//Thread.Sleep(100); 
					IsTaskExecuting = false;
					QueueLength--;
					NotifyQueueCountChanged();
				}
			}
				, (int)priority);
		}

		public byte[] AddCommandToQueueAndWaitExecution(IAddressedSendingItem item) {
			ISendResult result = new SendingResult(null, null, item);
			var eve = new AutoResetEvent(false);
			var qItem = new QueueItem {
				SendingItems = new List<IAddressedSendingItem> { item },
				OnComplete = results => {
					result = results.Count == 1 ? results[0] : new SendingResult(null, new Exception("Something wrong with channelSimple"), item);
					Log.Log("Установка сигнала для ожидающего потока...");
					eve.Set();
					Log.Log("Сигнал для ожидающего потока был установлен");
				}
			};

			AddCommandToQueueAndExecuteAsync(qItem);
			Log.Log("Ожидание сигнала от потока выполнения команд...");
			eve.WaitOne();
			Log.Log("Сигнал был установлен потоком выполнения команд");
			if (result.ChannelException != null) {
				Log.Log("В результате обмена было создано исключение, сейчас оно будет выброшено");
				throw result.ChannelException;
			}
			return result.Bytes;
		}

		private bool IsTaskExecuting {
			get {
				bool result;
				lock (_sync) {
					result = _isTaskExecuting;
				}
				return result;
			}
			set {
				lock (_sync) {
					_isTaskExecuting = value;
				}
			}
		}

		public int QueueLength {
			get {
				lock (_sync) {
					return _queueLength;
				}
			}
			private set {
				lock (_sync) {
					_queueLength = value;
				}
			}
		}

		public Action QueueChangedCallback {
			get {
				lock (_sync) {
					return _queueChangedCallback;
				}
			}
			set {
				lock (_sync) {
					_queueChangedCallback = value;
				}
			}
		}

		private void NotifyQueueCountChanged() {
			QueueChangedCallback?.Invoke();
		}

		public void ClearQueue() {
			_queueWorker.ClearQueue();
			NotifyQueueCountChanged();
		}

		private void RetrieveDNetIdFromNetworkAndPutItIntoStorage(ObjectAddress address, int dnetIdRetreiveTimeout) {
			Log.Log("Запрос DNetId из сети для адреса " + address + ", таймаут операции = " + dnetIdRetreiveTimeout + " сек.");
			var dNetId = _channelSimple.ResolveDNetIdByBroadcastAtModemCommand(address, _channelSimple.SNetId, dnetIdRetreiveTimeout); // Can throw timeout exc
			_addressMap.SetDNetId(address, dNetId);
			Log.Log("Полученное значение DNetID = " + dNetId);
		}

		private byte[] AskForData(ObjectAddress address, byte[] buffer, int attemptsCount, int waitTimeout) {
			try {
				Log.Log("Очередной запрос данных: " + buffer.ToText() + " для адреса " + address + ", количество попыток: " + attemptsCount + ", таймаут: " + waitTimeout + " сек.");
				return GetDnetIdFromStorageAndDoSendReceiveData(address, buffer, attemptsCount, waitTimeout);
			}
			catch (Exception ex) {
				Log.Log("Либо DNetId отсутствует в базе данных, либо ошибка связи по существующему DNetId, будет произведена попытка получения адреса из сети и повторный запрос, ошибка: " + ex);
				RetrieveDNetIdFromNetworkAndPutItIntoStorage(address, waitTimeout);
				return GetDnetIdFromStorageAndDoSendReceiveData(address, buffer, attemptsCount, waitTimeout);
			}
		}

		private byte[] GetDnetIdFromStorageAndDoSendReceiveData(ObjectAddress address, byte[] buffer, int attemptsCount, int waitTimeout) {
			var dNetId = _addressMap.GetDNetId(address);
			Log.Log("DNetId для адреса " + address + " найден в базе данных, значение = " + dNetId + ", будет произведен запрос");

			//ushort? inteleconAddress = (_checkInteleconAddress ? (ushort?) address.Value : null);
			var replyBytes = RequestSomethingCycle(dNetId, (_checkInteleconAddress ? (ushort?)address.Value : null), buffer, attemptsCount, waitTimeout);

			Log.Log("Запрос успешно произведен для адреса " + address + " (DNetId=" + dNetId + "), байты ответа: " + replyBytes.ToText());
			return replyBytes;
		}

		private byte[] RequestSomethingCycle(ushort address, ushort? netAddress, byte[] buffer, int attemptsCount, int waitTimeout) {
			Exception lastChannelException = null;
			int maxAttempts = attemptsCount;
			for (int i = 0; i < maxAttempts; ++i) {
				try {
					return _channelSimple.SendAndReceiveInteleconCommand(address, netAddress, buffer, waitTimeout);
				}
				catch (TimeoutException ex) {
					lastChannelException = ex;
				}
			}
			throw lastChannelException ?? new Exception("Не удалось осуществить обмен, однако и не удалось получить причину неудачи"); // Только что созданное исключение по идее не должно возникать, т.к. будет либо return, либо Timeout
		}

		public void Dispose() {
			_channelSimple.Dispose();
		}
	}
}
