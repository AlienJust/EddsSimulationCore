using System;
using Commands.Contracts;

namespace Commands.Bumiz.Intelecon {
	// TODO:
	public class ReadArchiveRecordServiceCommand1 : IInteleconCommand {
		public byte Code => 0x09;

		public string Comment => "Чтение данных архива через 9";

		public int RecordNumber { get; }

		public DateTime RequestedTime { get; }

		public byte RecordPart => 0;

		public ReadArchiveRecordServiceCommand1(DateTime time) {
			RequestedTime = time;
			RecordNumber =
				((time.Year - 2000) * 17856 + //* 2 * 12 * 31 * 24 +
				 (time.Month - 1) * 1488 + //*2*31*24 +
				 (time.Day - 1) * 48 + //*2*24 +
				 time.Hour * 2 +
				 (time.Minute < 30 ? 0 : 1)) % 4096;
		}

		public byte[] Serialize() {
			var result = new byte[4];
			result[0] = 0x05;
			result[1] = (byte)(RecordNumber & 0xFF);
			result[2] = (byte)((RecordNumber & 0xFF00) >> 8);
			result[3] = RecordPart;
			return result;
		}


		public IAdvancedArchiveResult1 GetFirstResult(byte[] reply) {
			int year = 2000 + ((reply[1] & 0xFE) >> 1);
			int month = ((reply[1] & 0x01) << 3) + ((reply[0] & 0xE0) >> 5);
			int day = reply[0] & 0x1F;

			DateTime? date;
			try {
				date = new DateTime(year, month, day);
			}
			catch {
				date = null;
			}


			var iaMean = reply[2];
			var inMean = reply[3];
			var iaPeak = reply[4];
			var inPeak = reply[5];
			var uaMean = reply[6];
			var uaPeak = reply[7];

			var t1 = reply[8] + reply[9] * 256 + reply[10] * 65536 + reply[11] * 16777216;
			var t2 = reply[12] + reply[13] * 256 + reply[14] * 65536 + reply[15] * 16777216;

			return new AdvancedArchiveResult1Simple {
				Date = date,
				IaMean = iaMean,
				IaPeak = iaPeak,
				InMean = inMean,
				InPeak = inPeak,
				T1 = t1,
				T2 = t2,
				UaMean = uaMean,
				UaPeak = uaPeak,
			};
		}
	}
}
