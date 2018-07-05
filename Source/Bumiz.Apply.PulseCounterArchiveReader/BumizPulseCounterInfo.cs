using BumizIoManager.Contracts;
using BumizNetwork.Contracts;

namespace Bumiz.Apply.PulseCounterArchiveReader {
  class BumizPulseCounterInfo : IBumizPulseCounterInfo {
    public IBumizObjectInfo BumizObjectInfo { get; set; }
    public IPulseCounterInfo PulseCounterInfo { get; set; }
    public IMonoChannel BumizChannel { get; set; }
  }
}