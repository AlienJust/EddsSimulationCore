using System;
using System.Collections.Generic;
using BumizNetwork.Contracts;

namespace BumizNetwork {
	class LinkQualityStorage : ILinkQualityStorage {
		private readonly Dictionary<ObjectAddress, DateTime?> _ot;
		public LinkQualityStorage() {
			_ot = new Dictionary<ObjectAddress, DateTime?>();
		}

		public DateTime? GetLastBadTime(ObjectAddress obj) {
			if (_ot.ContainsKey(obj)) {
				return _ot[obj];
			}
			return null;
		}

		public void SetLastBadTime(ObjectAddress obj, DateTime? time) {
			if (!_ot.ContainsKey(obj)) {
				_ot.Add(obj, time);
			}
			else {
				_ot[obj] = time;
			}
		}
	}
}