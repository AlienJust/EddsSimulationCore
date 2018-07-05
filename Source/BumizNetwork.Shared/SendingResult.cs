using System;
using BumizNetwork.Contracts;

namespace BumizNetwork.Shared {
  /// <summary>
  /// ����� � ����������
  /// </summary>
  public class SendingResult : ISendResult {
    public IAddressedSendingItem Request { get; }
    public byte[] Bytes { get; }
    public Exception ChannelException { get; }

    public SendingResult(byte[] bytes, Exception channelException, IAddressedSendingItem request) {
      Bytes = bytes;
      ChannelException = channelException;
      Request = request;
    }
  }
}