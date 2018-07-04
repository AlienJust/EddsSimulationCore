using System;
using Audience;
using Commands.Contracts;

namespace Commands.Bumiz.CounterSe102 {
	public class SetSpecialDayRecordCommand : ICounterCommand {
		public ushort Code => 0x0143;

		public string Comment => "Запись особых дат";

		private readonly byte _zeroBasedRecordNumber;
		private readonly byte _dayNumber;
		private readonly byte _monthNumber;
		private readonly TariffProgramType _tariffProgram;
		public SetSpecialDayRecordCommand(byte zeroBasedRecordNumber, byte dayNumber, byte monthNumber, TariffProgramType tariffProgram) {
			if (zeroBasedRecordNumber > 31) throw new IndexOutOfRangeException("Record number must be less than 32 (0..31)");

			_zeroBasedRecordNumber = zeroBasedRecordNumber;
			/*if (dayNumber > 0 && monthNumber > 0)
			{
				_dayNumber = (byte) (dayNumber + 48);
				_monthNumber = (byte) (monthNumber + 48);
			}
			else
			{
				_dayNumber = 0;
				_monthNumber = 0;
			}*/
			_dayNumber = dayNumber.BinaryToBcd();
			_monthNumber = monthNumber.BinaryToBcd();
			_tariffProgram = tariffProgram;


		}

		public byte[] Serialize() {

			return new[] { _zeroBasedRecordNumber, _dayNumber, _monthNumber, (byte)_tariffProgram };
		}

		public int AwaitedBytesCount => 0;
	}
}
