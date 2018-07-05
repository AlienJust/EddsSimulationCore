using System;

namespace Bumiz.Apply.PulseCounterArchiveReader {
  class PulseCounterInfo : IPulseCounterInfo {
    public string Name { get; }
    public DateTime SetupTime { get; }

    public PulseCounterInfo(string name, DateTime setupTime) {
      Name = name;
      SetupTime = setupTime;
    }
  }
}