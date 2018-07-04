using System;
using System.Collections.Generic;
using System.Threading;
using AJ.Std.Concurrent;
using AJ.Std.Concurrent.Contracts;

namespace Bumiz.Apply.PulseCounterArchiveReader {
	internal class ConcurentPulseCounterDataStorage : IPulseCounterDataStorage {
		private readonly IPulseCounterDataStorage _storage;
		private readonly IWorker<Action> _queueWorker;

		public ConcurentPulseCounterDataStorage(IPulseCounterDataStorage storage) {
			_storage = storage;
			_queueWorker = new SingleThreadedRelayQueueWorkerProceedAllItemsBeforeStopNoLog<Action>("BumizPulseCounterArchiveReaderQueueThread", a => a(), ThreadPriority.Normal, true, null);
		}

		public IIntegralData GetIntegralData(string objectName, DateTime upToTime) {
			IIntegralData result = null;
			Exception exc = null;
			_queueWorker.AddToQueueAndWaitExecution(() => {
				try {
					result = _storage.GetIntegralData(objectName, upToTime);
				}
				catch (Exception ex) {
					exc = ex;
				}
			});
			if (exc != null) throw exc;
			return result;
		}

		public List<DateTime> GetMissedTimesUpToTime(string objectName, DateTime nowTime) {
			List<DateTime> result = null;
			Exception exc = null;
			_queueWorker.AddToQueueAndWaitExecution(() => {
				try {
					result = _storage.GetMissedTimesUpToTime(objectName, nowTime);
				}
				catch (Exception ex) {
					exc = ex;
				}
			});
			if (exc != null) throw exc;
			return result;
		}

		public DateTime? GetFirstMissedTimeUpToTime(string objectName, DateTime nowTime) {
			DateTime? result = null;
			Exception exc = null;
			_queueWorker.AddToQueueAndWaitExecution(() => {
				try {
					result = _storage.GetFirstMissedTimeUpToTime(objectName, nowTime);
				}
				catch (Exception ex) {
					exc = ex;
				}

			});
			if (exc != null) throw exc;
			return result;
		}

		public void SaveData(string objectName, DateTime time, bool isRecordCorrect, int pulseCount1, int pulseCount2, int pulseCount3, int status, int statusX) {
			Exception exc = null;
			_queueWorker.AddToQueueAndWaitExecution(() => {
				try {
					_storage.SaveData(objectName, time, isRecordCorrect, pulseCount1, pulseCount2, pulseCount3, status, statusX);
				}
				catch (Exception ex) {
					exc = ex;
				}
			});
			if (exc != null) throw exc;
		}

		public AtomRec? GetAtomicData(string objectName, DateTime certainTime) {
			AtomRec? result = null;
			Exception exc = null;
			_queueWorker.AddToQueueAndWaitExecution(() => {
				try {
					result = _storage.GetAtomicData(objectName, certainTime);
				}
				catch (Exception ex) {
					exc = ex;
				}
			});
			if (exc != null) throw exc;
			return result;
		}
	}
}