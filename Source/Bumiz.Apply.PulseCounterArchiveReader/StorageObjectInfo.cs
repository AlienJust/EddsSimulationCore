using System;
using System.Collections.Generic;

namespace Bumiz.Apply.PulseCounterArchiveReader {
	class StorageObjectInfo {
		public string ObjectName { get; }
		public Dictionary<DateTime, AtomRec> FileRecords { get; }
		public DateTime SetupTime { get; }

		public StorageObjectInfo(string objectName, DateTime setupTime) {
			ObjectName = objectName;
			SetupTime = setupTime;
			FileRecords = new Dictionary<DateTime, AtomRec>();
		}
	}
}