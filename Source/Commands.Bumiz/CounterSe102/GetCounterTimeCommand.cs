using System;
using Commands.Contracts;

namespace Commands.Bumiz.CounterSe102 {
	public class GetCounterTimeCommand : ICounterCommand {
		public ushort Code => 0x0120;

		public string Comment => "Чтение времени счётчика";

		public byte[] Serialize() {
			return new byte[0];
		}

		public int AwaitedBytesCount => 7;

		public DateTime GetResult(byte[] reply) {
			// проверка на глюк Ромы (когда только прошивка была прошита, без установки даты месяц  = 0 :O)
			if (reply[5] == 0) return new DateTime(2001, 1, 1, 1, 15, 30);
			return new DateTime
				(
				(2000 + int.Parse(reply[6].ToString("X2"))),
				int.Parse(reply[5].ToString("X2")),
				int.Parse(reply[4].ToString("X2")),
				int.Parse(reply[2].ToString("X2")),
				int.Parse(reply[1].ToString("X2")),
				int.Parse(reply[0].ToString("X2"))
				);
		}
	}
}
