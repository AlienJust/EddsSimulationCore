namespace PollServiceProxy.Contracts {
  /// <summary>
  /// Base gateway app interface 
  /// </summary>
  public interface IInteleconGateway {
    void SendDataInstantly(string scadaObjectName, byte commandCode, byte[] data);
    void RegisterSubSystem(ISubSystem subSystem);
  }
}