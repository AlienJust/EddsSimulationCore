using System;
using Commands.Contracts;

namespace Commands.Bumiz.Intelecon {
	// TODO:
	public class ReadArchiveRecordServiceCommand : IInteleconCommand {
		//i8 requc27[12]="\x7A\x07\x09\x00\xFF\x05\x00\x00\x00\x00\x0D";
		//requc27[6]=num&0xff;
		//requc27[7]=num>>8L;

		public byte Code => 0x09;

		public string Comment => "Чтение данных архива через 9";

		public int RecordNumber { get; }

		public DateTime RequestedTime { get; }

		public ReadArchiveRecordServiceCommand(DateTime time) {
			RequestedTime = time;
			RecordNumber =
				((time.Year - 2000)*17856 + //* 2 * 12 * 31 * 24 +
				 (time.Month - 1)*1488 + //*2*31*24 +
				 (time.Day - 1)*48 + //*2*24 +
				 time.Hour * 2 + 
				 (time.Minute < 30 ? 0 : 1)) % 8192;
		}

		public byte[] Serialize() {
			var result = new byte[3];
			result[0] = 0x05;
			result[1] = (byte) (RecordNumber & 0xFF);
			result[2] = (byte) ((RecordNumber & 0xFF00) >> 8);
			return result;
		}


		public ReadArchiveCommandResult GetResult(byte[] reply) {
			// TODO: move msdos format conversion datetime to audience
			int year = 2000 + ((reply[1] & 0xFE) >> 1);
			int month = ((reply[1] & 0x01) << 3) + ((reply[0] & 0xE0) >> 5);
			int day = (reply[0] & 0x1F);

			int hour = ((reply[3] & 0xF8) >> 3);
			int minute = ((reply[3] & 0x07) << 3) + ((reply[2] & 0xE0) >> 5);
			int second = (reply[2] & 0x1F);

			var dt = new DateTime(year, month, day, hour, minute, second);
			var xstatus = reply[4] + reply[5] * 256;
			var status = reply[6];

			var c1 = reply[7] + reply[8] * 256 + reply[9] * 65536;
			var c2 = reply[10] + reply[11] * 256 + reply[12] * 65536;
			var c3 = reply[13] + reply[14] * 256 + reply[15] * 65536;

			return new ReadArchiveCommandResult(dt, xstatus, status, c1, c2, c3);
		}

		public ReadArchiveCommandResultSafe GetSafeResult(byte[] reply)
		{
			// TODO: move msdos format conversion datetime to audience
			int year = 2000 + ((reply[1] & 0xFE) >> 1);
			int month = ((reply[1] & 0x01) << 3) + ((reply[0] & 0xE0) >> 5);
			int day = (reply[0] & 0x1F);

			int hour = ((reply[3] & 0xF8) >> 3);
			int minute = ((reply[3] & 0x07) << 3) + ((reply[2] & 0xE0) >> 5);
			int second = (reply[2] & 0x1F);

			DateTime? dt;
			try {
				dt = new DateTime(year, month, day, hour, minute, second);
			}
			catch {
				dt = null;
			}
			
			var xstatus = reply[4] + reply[5] * 256;
			var status = reply[6];

			var c1 = reply[7] + reply[8] * 256 + reply[9] * 65536;
			var c2 = reply[10] + reply[11] * 256 + reply[12] * 65536;
			var c3 = reply[13] + reply[14] * 256 + reply[15] * 65536;

			return new ReadArchiveCommandResultSafe(dt, xstatus, status, c1, c2, c3);
		}
	}
}
