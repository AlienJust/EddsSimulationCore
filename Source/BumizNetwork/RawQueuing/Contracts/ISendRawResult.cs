using System;
using System.Collections.Generic;

namespace BumizNetwork.RawQueuing.Contracts {
  public interface ISendRawResult {
    IEnumerable<byte> Bytes { get; }
    Exception ChannelException { get; }

    // ���� Bytes = 0 � ChannelException == null, ������ 
  }
}