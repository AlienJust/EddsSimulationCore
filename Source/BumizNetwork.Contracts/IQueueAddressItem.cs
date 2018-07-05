using System;
using System.Collections.Generic;

namespace BumizNetwork.Contracts {
  public interface IQueueAddressItem {
    /// <summary>
    /// ����� ������� � ������ ������!
    /// </summary>
    Action<List<ISendResult>> OnComplete { get; }

    List<IAddressedSendingItem> SendingItems { get; }
  }
}