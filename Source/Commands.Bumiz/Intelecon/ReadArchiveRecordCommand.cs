using System;
using Commands.Contracts;

namespace Commands.Bumiz.Intelecon
{
	// TODO:
	public class ReadArchiveRecordCommand : IInteleconCommand
	{
		public byte Code => 0x07;

		public string Comment => "Чтение данных архива";

		public int RecordNumber { get; }

		public DateTime RequestedTime { get; }

		public ReadArchiveRecordCommand(DateTime time)
		{
			RequestedTime = time;
			RecordNumber =
				((time.Year - 2000) * 17856  + //* 2 * 12 * 31 * 24 +
				(time.Month - 1) * 1488+//*2*31*24 +
				(time.Day - 1) * 48+//*2*24 +
				time.Hour * 2 + 
				(time.Minute < 30 ? 0 : 1)) % 8192;
		}

		public byte[] Serialize() {
			var result = new byte[2];
			result[0] = (byte)(RecordNumber & 0xFF);
			result[1] = (byte) ((RecordNumber & 0xFF00) >> 8);
			return result;
		}

		public ReadArchiveCommandResult GetResult(byte[] reply)
		{
			// TODO: current data reply is 14 bytes!
			int year = 2000 + ((reply[1] & 0xFE) >> 1);
			int month = ((reply[1] & 0x01) << 3) + ((reply[0] & 0xE0) >> 5);
			int day = (reply[0] & 0x1F);

			int hour = ((reply[3] & 0xF8) >> 3);
			int minute = ((reply[3] & 0x07) << 3) + ((reply[2] & 0xE0) >> 5);
			int second = (reply[2] & 0x1F);

			var dt = new DateTime(year, month, day, hour, minute, second);
			var xstatus = reply[4] + reply[5]*256;
			var status = reply[6];

			var c1 = reply[7] + reply[8]*256 + reply[9]*65536;
			var c2 = reply[10] + reply[11] * 256 + reply[12] * 65536;
			var c3 = reply[13] + reply[14] * 256 + reply[15] * 65536;

			return new ReadArchiveCommandResult(dt, xstatus, status, c1, c2, c3);
		}
	}

	public struct ReadArchiveCommandResult {
		public readonly DateTime RecordTime;
		public readonly int Xstatus;
		public readonly int Status;
		public readonly int Count1;
		public readonly int Count2;
		public readonly int Count3;

		public ReadArchiveCommandResult(DateTime rTime, int xStatus, int status, int c1, int c2, int c3) {
			RecordTime = rTime;
			Xstatus = xStatus;
			Status = status;
			Count1 = c1;
			Count2 = c2;
			Count3 = c3;
		}

		public override string ToString() {
			string result = string.Empty;
			result += "Дата: \t" + RecordTime.ToString("yyyy.MM.dd HH:mm:ss") + Environment.NewLine;
			result += "XStatus: \t" + Xstatus + Environment.NewLine;
			result += "Status: \t" + Status + Environment.NewLine;
			result += "Счетчик 1: \t" + Count1 + Environment.NewLine;
			result += "Счетчик 2: \t" + Count2 + Environment.NewLine;
			result += "Счетчик 3: \t" + Count3 + Environment.NewLine;
			return result;
		}
	}

	public struct ReadArchiveCommandResultSafe
	{
		public readonly DateTime? RecordTime;
		public readonly int Xstatus;
		public readonly int Status;
		public readonly int Count1;
		public readonly int Count2;
		public readonly int Count3;

		public ReadArchiveCommandResultSafe(DateTime? rTime, int xStatus, int status,int c1, int c2, int c3)
		{
			RecordTime = rTime;
			Xstatus = xStatus;
			Status = status;
			Count1 = c1;
			Count2 = c2;
			Count3 = c3;
		}

		public override string ToString()
		{
			string result = string.Empty;
			if (RecordTime.HasValue)
				result += "Дата: \t" + RecordTime.Value.ToString("yyyy.MM.dd HH:mm:ss") + Environment.NewLine;
			else 
				result += "Дата: \t Неизвестно" + Environment.NewLine;
			result += "XStatus: \t" + Xstatus + Environment.NewLine;
			result += "Status: \t" + Status + Environment.NewLine;
			result += "Счетчик 1: \t" + Count1 + Environment.NewLine;
			result += "Счетчик 2: \t" + Count2 + Environment.NewLine;
			result += "Счетчик 3: \t" + Count3 + Environment.NewLine;
			return result;
		}
	}
}
