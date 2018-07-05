namespace PollServiceProxy.Contracts {
  /// <summary>
  /// ������������ ��������� ���������� ���� ��� �������� ������
  /// </summary>
  public interface IPollGateway {
    void SendData(string uplinkName, string scadaObjectName, byte commandCode, byte[] data);
    void RegisterSubSystem(ISubSystem subSystem);
  }
}