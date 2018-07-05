using System.Collections.Generic;

namespace PollServiceProxy {
  internal sealed class ScadaObjectInfo : IScadaObjectInfo {
    public string Name { get; set; }
    public bool SendMicroPackets { get; set; }
    public IEnumerable<IScadaAddress> ScadaAddresses { get; set; }
  }
}