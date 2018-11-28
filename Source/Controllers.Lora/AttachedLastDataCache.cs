using System;
using System.Collections.Generic;

namespace Controllers.Lora {
	public class AttachedLastDataCache : IAttachedLastDataCache {
		private readonly Dictionary<string, Dictionary<int, Tuple<DateTime, byte[]>>> _wholeDataCollection;

		public AttachedLastDataCache() {
			_wholeDataCollection = new Dictionary<string, Dictionary<int, Tuple<DateTime, byte[]>>>();
		}
		
		public void AddData(string controllerId, int config, byte[] data) {
			if (!_wholeDataCollection.ContainsKey(controllerId)) {
				_wholeDataCollection.Add(controllerId, new Dictionary<int, Tuple<DateTime, byte[]>>());
			}

			if (!_wholeDataCollection[controllerId].ContainsKey(config)) {
				_wholeDataCollection[controllerId].Add(config, new Tuple<DateTime, byte[]>(DateTime.Now, data));
			}
			else _wholeDataCollection[controllerId][config] = new Tuple<DateTime, byte[]>(DateTime.Now, data);
		}

		public Tuple<DateTime, byte[]> GetData(string controllerId, int config) {
			try {
				return _wholeDataCollection[controllerId][config];
			}
			catch (Exception e) {
				Console.WriteLine(e);
				throw new CannotGetDataFromCacheException("Cannot get data from cache", e);
			}
			
		}
	}

	public class CannotGetDataFromCacheException : Exception {
		public CannotGetDataFromCacheException(string message, Exception innerException) : base(message, innerException) {
			
		}
	}
}