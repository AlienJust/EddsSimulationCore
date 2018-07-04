using System;

namespace Commands.Bumiz.Intelecon {
	class AdvancedArchiveResult3Simple : IAdvancedArchiveResult3
	{
		public TimeSpan Time { get; set; }
		public int HotWater1 { get; set; }
		public int HotWater2 { get; set; }
		public int ColdWater1 { get; set; }
		public int ColdWater2 { get; set; }
		public int Gas1 { get; set; }
		public int Gas2 { get; set; }
		public int Xstatus { get; set; }
		public int R45 { get; set; }
		public int R47 { get; set; }
		public int R46 { get; set; }
	}
}