using BumizIoManager.Contracts;
using BumizNetwork.Contracts;

namespace Bumiz.Apply.PulseCounterArchiveReader {
  internal interface IBumizPulseCounterInfo {
    IBumizObjectInfo BumizObjectInfo { get; }
    IPulseCounterInfo PulseCounterInfo { get; }
    IMonoChannel BumizChannel { get; }
  }
}