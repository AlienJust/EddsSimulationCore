namespace Bumiz.Apply.PulseCounterArchiveReader {
	class IntegralData : IIntegralData {
		public IntegralData(int impulsesCount1, int impulsesCount2, int impulsesCount3, int recordsCount, int correctRecordsCount, int incorrectRecordsCount, int supposedRecordsCount) {
			ImpulsesCount1 = impulsesCount1;
			ImpulsesCount2 = impulsesCount2;
			ImpulsesCount3 = impulsesCount3;
			RecordsCount = recordsCount;
			CorrectRecordsCount = correctRecordsCount;
			IncorrectRecordsCount = incorrectRecordsCount;
			SupposedRecordsCount = supposedRecordsCount;
		}

		public int ImpulsesCount1 { get; }

		public int ImpulsesCount2 { get; }

		public int ImpulsesCount3 { get; }

		public int RecordsCount { get; }

		public int CorrectRecordsCount { get; }

		public int IncorrectRecordsCount { get; }

		public int SupposedRecordsCount { get; }
	}
}