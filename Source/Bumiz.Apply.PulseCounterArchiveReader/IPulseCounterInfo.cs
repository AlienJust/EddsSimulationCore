using System;

namespace Bumiz.Apply.PulseCounterArchiveReader {
	interface IPulseCounterInfo {
		string Name { get; }
		DateTime SetupTime { get; }
	}
}