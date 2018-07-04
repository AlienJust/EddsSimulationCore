using System;
using System.Collections.Generic;

namespace BumizIoManager {
	class StorageObjectInfo {
		public string Name { get; private set; }
		public List<DateTime> MissedRecords { get; private set; }
		public int Count1 { get; private set; }
		public int Count2 { get; private set; }
		public int Count3 { get; private set; }
	}
}