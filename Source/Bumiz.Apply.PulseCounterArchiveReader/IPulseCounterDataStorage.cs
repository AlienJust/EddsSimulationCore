using System;
using System.Collections.Generic;

namespace Bumiz.Apply.PulseCounterArchiveReader {
	public interface IPulseCounterDataStorage {
		IIntegralData GetIntegralData(string objectName, DateTime upToTime); // TODO: can be moved to another interface, cause it used to give data to upper system
		AtomRec? GetAtomicData(string objectName, DateTime certainTime);

		List<DateTime> GetMissedTimesUpToTime(string objectName, DateTime nowTime);
		DateTime? GetFirstMissedTimeUpToTime(string objectName, DateTime nowTime);

		void SaveData(string objectName, DateTime time, bool isRecordCorrect, int pulseCount1, int pulseCount2, int pulseCount3, int status, int statusX);
	}
}