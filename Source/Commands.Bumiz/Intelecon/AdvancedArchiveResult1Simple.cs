using System;

namespace Commands.Bumiz.Intelecon
{
	internal class AdvancedArchiveResult1Simple : IAdvancedArchiveResult1 {
		public DateTime? Date { get; set; }
		public int IaMean { get; set; }
		public int InMean { get; set; }
		public int IaPeak { get; set; }
		public int InPeak { get; set; }
		public int UaMean { get; set; }
		public int UaPeak { get; set; }
		public int T1 { get; set; }
		public int T2 { get; set; }
	}
}