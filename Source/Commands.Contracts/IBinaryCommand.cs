namespace Commands.Contracts
{
	public interface IBinaryCommand
	{
		/// <summary>
		/// Комментарий (например название команды)
		/// </summary>
		string Comment { get; }

		/// <summary>
		/// Сериализует команду в байты
		/// </summary>
		/// <returns></returns>
		byte[] Serialize();

	}

	/*
	/// <summary>
	/// Команда, длина ответа на которую известна
	/// </summary>
	public interface IBackLenKnownCommand : IBinaryCommand
	{
		int AwaitedBytesCount { get; }
	}*/

	public interface ICounterCommand : IBinaryCommand
	{
		ushort Code { get; }
	}

	public interface IInteleconCommand : IBinaryCommand
	{
		byte Code { get; }
	}
}
