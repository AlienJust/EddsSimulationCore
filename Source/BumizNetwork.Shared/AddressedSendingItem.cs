using BumizNetwork.Contracts;

namespace BumizNetwork.Shared {
  /// <summary>
  /// ������ ��� ��������
  /// </summary>
  public sealed class AddressedSendingItem : IAddressedSendingItem {
    public ObjectAddress Address { get; set; }
    public byte[] Buffer { get; set; }
    public int AttemptsCount { get; set; }
    public int WaitTimeout { get; set; }
  }
}