using System;

namespace Commands.Bumiz.Intelecon {
	public interface IAdvancedArchiveResult1 {
		DateTime? Date { get; }
		int IaMean { get; }
		int InMean { get; }
		int IaPeak { get; }
		int InPeak { get; }
		int UaMean { get; }
		int UaPeak { get; }
		int T1 { get; }
		int T2 { get; }
	}
}