using System;
using Audience;
using Commands.Contracts;

namespace Commands.Bumiz.CounterSe102 {
	public class GetSpecialDayRecordCommand : ICounterCommand {
		public ushort Code => 0x0142;

		public string Comment => "Чтение особых дат";

		private readonly byte _zeroBasedRecordNumber;
		public GetSpecialDayRecordCommand(byte zeroBasedRecordNumber) {
			if (zeroBasedRecordNumber > 31) throw new IndexOutOfRangeException("Record number must be less 32 (0..31)");
			_zeroBasedRecordNumber = zeroBasedRecordNumber;
		}

		public byte[] Serialize() {
			return new[] { _zeroBasedRecordNumber };
		}

		public int AwaitedBytesCount => 3;

		public GetSpecialDayRecordResult GetResult(byte[] reply) {
			return new GetSpecialDayRecordResult(reply[0].BcdToBinary(), reply[1].BcdToBinary(), (TariffProgramType)reply[2]);
		}
	}
	public struct GetSpecialDayRecordResult {
		public readonly int Month;
		public readonly int Day;
		public readonly TariffProgramType Program;
		public GetSpecialDayRecordResult(int day, int month, TariffProgramType program) {
			Day = day;
			Month = month;
			Program = program;
		}
	}
}
