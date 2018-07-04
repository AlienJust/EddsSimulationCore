using System;

namespace ScadaClient.Contracts
{
	public interface IScadaClient
	{
		/// <summary>
		/// Отправляет данные
		/// </summary>
		/// <param name="netAddress">Сетевой адрес</param>
		/// <param name="commandCode">Код команды</param>
		/// <param name="data">Данные</param>
		void SendData(ushort netAddress, byte commandCode, byte[] data);


		/// <summary>
		/// Отправляет микропакет
		/// </summary>
		/// <param name="netAddress">Сетевой адрес</param>
		/// <param name="linkLevel">Уровень связи (15-30)</param>
		void SendMicroPacket(ushort netAddress, byte linkLevel);


		/// <summary>
		/// Возникает при приёме данных
		/// </summary>
		event EventHandler<DataReceivedEventArgs> DataReceived;


		/// <summary>
		/// Возникает при отключении клиента
		/// </summary>
		event EventHandler<DisconnectedEventArgs> Disconnected;
	}
}
