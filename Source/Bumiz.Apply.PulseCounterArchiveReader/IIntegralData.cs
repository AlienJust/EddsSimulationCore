namespace Bumiz.Apply.PulseCounterArchiveReader {
	public interface IIntegralData {
		int ImpulsesCount1 { get; }
		int ImpulsesCount2 { get; }
		int ImpulsesCount3 { get; }

		int RecordsCount { get; }
		int CorrectRecordsCount { get; }
		int IncorrectRecordsCount { get; }
		int SupposedRecordsCount { get; }
	}
}