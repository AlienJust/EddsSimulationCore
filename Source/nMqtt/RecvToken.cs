using System.Collections.Generic;

namespace nMqtt {
  internal sealed class RecvToken {
    public List<byte> Buffer { get; } = new List<byte>();

    private int Count {
      get {
        if (Buffer != null && Buffer.Count >= 2) {
          int offset = 1;
          byte encodedByte;
          var multiplier = 1;
          var remainingLength = 0;

          do {
            encodedByte = Buffer[offset];
            remainingLength += encodedByte & 0x7f * multiplier;
            multiplier *= 0x80;
          } while ((++offset <= 4) && (encodedByte & 0x80) != 0);

          return remainingLength + offset;
        }

        return 0;
      }
    }

    /// <summary>
    /// A boolean that indicates whether the message read is complete 
    /// </summary>
    public bool IsReadComplete => Buffer.Count >= Count;

    public void Reset() {
      Buffer.Clear();
    }
  }
}