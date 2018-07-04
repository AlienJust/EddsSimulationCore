using Commands.Contracts;

namespace Commands.Bumiz.Intelecon
{
	public class TurnOnOffCommand : IInteleconCommand
	{
		private readonly bool _turnOnIfTrue;

		public TurnOnOffCommand(bool turnOnIfTrue)
		{
			_turnOnIfTrue = turnOnIfTrue;
		}

		public byte Code => 0x05;

		public string Comment => _turnOnIfTrue ? "Включение автомата" : "Отключение автомата";

		public byte[] Serialize()
		{
			var query = new byte[] {0x02};
			if (_turnOnIfTrue) query[0] = 0x01;
			
			return query;
		}
	}
}
