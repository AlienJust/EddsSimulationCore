using System;

namespace Bumiz.Apply.PulseCounterArchiveReader {
	public interface IObjectData {
		IIntegralData GetIntegralData(DateTime upToTime);
		AtomRec GetAtomicDataForTime(DateTime time);
		bool AddRecord(DateTime time, AtomRec data);

		DateTime SetupTime { get; }
		string ObjectName { get; }
		bool ContatinsDataForTime(DateTime time);
	}
}