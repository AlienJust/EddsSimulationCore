using System;
using System.Collections.Generic;
using System.IO;

namespace nMqtt.Messages {
  /// <summary>
  /// The message header for each MQTT command message contains a fixed header (2 bytes)
  /// </summary>
  public class FixedHeader
  {
    /// <summary>
    /// Message Type (b1.7-4)
    /// </summary>
    public MessageType MessageType { get; set; }
    
    /// <summary>
    /// Duplicate delivery (b1.3)
    /// </summary>
    public bool Dup { get; set; }
    
    /// <summary>
    /// Quality of Service (b1.2-1)
    /// </summary>
    public Qos Qos { get; set; }
    
    /// <summary>
    /// RETAIN flag (b1.0) This flag is only used on PUBLISH messages
    /// </summary>
    public bool Retain { get; set; }
    
    /// <summary>
    /// Remaining Length (byte 2) up to 127 bytes long OR more (see mqtt spec)
    /// </summary>
    public int RemaingLength { get; set; }

    public FixedHeader(MessageType msgType)
    {
      MessageType = msgType;
    }

    public FixedHeader(Stream stream)
    {
      if (stream.Length < 2)
        throw new Exception("The supplied header is invalid. Header must be at least 2 bytes long.");

      var byte1 = stream.ReadByte();
      MessageType = (MessageType)((byte1 & 0xf0) >> 4);
      Dup = (byte1 & 0x08) >> 3 > 0;
      Qos = (Qos)((byte1 & 0x06) >> 1);
      Retain = (byte1 & 0x01) > 0;

      RemaingLength = DecodeLenght(stream);
    }

    public void WriteTo(Stream stream)
    {
      var flags = (byte)MessageType << 4;
      flags |= Dup.ToByte() << 3;
      flags |= (byte)Qos << 1;
      flags |= Retain.ToByte();

      Console.WriteLine("Fixed header first byte (bin) = " + Convert.ToString((byte) flags, 2).PadLeft(8, '0'));
      stream.WriteByte((byte)flags);                
      stream.Write(EncodeLength(RemaingLength));   
    }

    private static byte[] EncodeLength(int length)
    {
      var result = new List<byte>();
      do
      {
        var digit = (byte)(length % 0x80);
        length /= 0x80;
        if (length > 0)
          digit |= 0x80;
        result.Add(digit);
      } while (length > 0);

      return result.ToArray();
    }

    private static int DecodeLenght(Stream stream)
    {
      byte encodedByte;
      var multiplier = 1;
      var remainingLength = 0;
      do
      {
        encodedByte = (byte)stream.ReadByte();
        remainingLength += (encodedByte & 0x7f) * multiplier;
        multiplier *= 0x80;
      } while ((encodedByte & 0x80) != 0);

      return remainingLength;
    }
  }
}