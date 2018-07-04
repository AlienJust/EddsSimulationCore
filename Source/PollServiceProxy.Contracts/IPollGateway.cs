namespace PollServiceProxy.Contracts {
	/// <summary>
	/// Используется объектами внутренней сети для отправки данных
	/// </summary>
	public interface IPollGateway {
		void SendData(string uplinkName, string scadaObjectName, byte commandCode, byte[] data);
		void RegisterSubSystem(ISubSystem subSystem);
	}
}