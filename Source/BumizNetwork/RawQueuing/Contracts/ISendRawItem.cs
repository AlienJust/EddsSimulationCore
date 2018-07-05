using System.Collections.Generic;

namespace BumizNetwork.RawQueuing.Contracts {
  public interface ISendRawItem {
    IEnumerable<byte> SendingBytes { get; }
    int? AwaitedBytesCount { get; }
  }
}