namespace SubChannel.Contracts {
  public delegate void DataReceivedDelegate(ushort netAddress, byte commandCode, byte[] data);

  public interface ISubChannel {
    /// <summary>
    /// Sends data
    /// </summary>
    /// <param name="netAddress">Сетевой адрес</param>
    /// <param name="commandCode">Код команды</param>
    /// <param name="data">Данные</param>
    void SendDataSync(ushort netAddress, byte commandCode, byte[] data);

    /// <summary>
    /// Возникает при приёме данных
    /// </summary>
    event DataReceivedDelegate DataReceived;
  }
}