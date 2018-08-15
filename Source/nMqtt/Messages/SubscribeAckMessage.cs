using System.IO;

namespace nMqtt.Messages {
  /// <summary>
  /// 订阅回执
  /// </summary>
  [MessageType(MessageType.Suback)]
  public class SubscribeAckMessage : MqttMessage
  {
    public short MessageIdentifier { get; set; }

    protected override void Decode(Stream stream)
    {
      MessageIdentifier = stream.ReadShort();
    }
  }
}