using System.Collections.Generic;
using Commands.Contracts;

namespace Commands.Bumiz.Counters.Mnemonic
{
	public class SetAutorizationStatusCommand : ICounterCommand {
		private readonly bool _isAutorizated;

		public SetAutorizationStatusCommand(bool isAutorizted)
		{
			_isAutorizated = isAutorizted;
		}

		public ushort Code => 0x0FFA;

		public string Comment => _isAutorizated? "Авторизация" : "Завершение сеанса";


		public byte[] Serialize() {
			return new [] {_isAutorizated ? (byte) 1 : (byte) 0};
		}


		public int AwaitedBytesCount => 0;

		public bool IsCommandSucceed(IList<byte> reply)
		{
			if (reply[0] == 0x06) return true;
			return false;
		}
	}
}
