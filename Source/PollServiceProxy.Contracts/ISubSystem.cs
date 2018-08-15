using System;
using System.Collections.Generic;

namespace PollServiceProxy.Contracts {
  /// <summary>
  /// Subsystem interface, subsystems can receive data and send back replies
  /// </summary>
  public interface ISubSystem {
    /// <summary>
    /// SubSystem name
    /// </summary>
    string SystemName { get; }

    /// <summary>
    /// When receiving data
    /// </summary>
    /// <param name="uplinkName">SCADA link name</param>
    /// <param name="subObjectName">SCADA object name</param>
    /// <param name="commandCode">Intlecon (or not only Intelecon) protocol command code</param>
    /// <param name="data">Command data bytes</param>
    /// <param name="notifyOperationComplete">Callback about operation proceed complete</param>
    /// <param name="sendReplyAction">Send back reply action</param>
    void ReceiveData(string uplinkName, string subObjectName, byte commandCode, IReadOnlyList<byte> data,
      Action notifyOperationComplete, Action<int, IReadOnlyList<byte>> sendReplyAction);
  }
}