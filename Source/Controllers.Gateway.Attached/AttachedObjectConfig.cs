namespace Controllers.Gateway.Attached {
  struct AttachedObjectConfig {
    public AttachedObjectConfig(string gateway, int channel, int type, int number) {
      Gateway = gateway;
      Channel = channel;
      Type = type;
      Number = number;
    }
    string Gateway { get; }
    int Channel { get; }
    int Type { get; }
    int Number { get; }

    public override bool Equals(object obj) {
      if (obj is AttachedObjectConfig aoc) {
        return Equals(aoc);
      }
     return false;
    }

    private bool Equals(AttachedObjectConfig other) {
      return string.Equals(Gateway, other.Gateway) && Channel == other.Channel && Type == other.Type && Number == other.Number;
    }

    public override int GetHashCode() {
      unchecked {
        var hashCode = Gateway != null ? Gateway.GetHashCode() : 0;
        hashCode = (hashCode * 397) ^ Channel;
        hashCode = (hashCode * 397) ^ Type;
        hashCode = (hashCode * 397) ^ Number;
        return hashCode;
      }
    }
  }
}