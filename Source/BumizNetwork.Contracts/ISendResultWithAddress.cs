using System;

namespace BumizNetwork.Contracts {
  public interface ISendResultWithAddress {
    IAddressedSendingItem Request { get; }
    byte[] Bytes { get; }
    Exception ChannelException { get; }
    ushort AddressInReply { get; }
  }
}