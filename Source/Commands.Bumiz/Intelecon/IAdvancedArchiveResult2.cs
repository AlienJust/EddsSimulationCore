namespace Commands.Bumiz.Intelecon {
	public interface IAdvancedArchiveResult2 {

		int IbMean { get; }
		int IcMean { get; }
		int IbPeak { get; }
		int IcPeak { get; }
		int UbMean { get; }
		int UcMean { get; }
		int UbPeak { get; }
		int UcPeak { get; }

		int T3 { get; }
		int T4 { get; }
	}
}