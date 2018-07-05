using System.Collections.Generic;

namespace PollServiceProxy {
  internal class ScadaAddress : IScadaAddress {
    public string LinkName { get; set; }
    public int NetAddress { get; set; }

    public override bool Equals(object obj) {
      if (ReferenceEquals(null, obj)) return false;
      if (ReferenceEquals(this, obj)) return true;
      if (obj.GetType() != typeof(ScadaAddress)) return false;
      return Equals((ScadaAddress) obj);
    }

    public bool Equals(ScadaAddress other) {
      if (ReferenceEquals(null, other)) return false;
      if (ReferenceEquals(this, other)) return true;
      return Equals(other.LinkName, LinkName) && other.NetAddress == NetAddress;
    }

    public override int GetHashCode() {
      unchecked {
        return ((LinkName != null ? LinkName.GetHashCode() : 0) * 397) ^ NetAddress;
      }
    }

    public override string ToString() {
      return NetAddress + "@" + LinkName;
    }
  }
}