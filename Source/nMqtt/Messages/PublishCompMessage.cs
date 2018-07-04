using System.IO;

namespace nMqtt.Messages {
  /// <summary>
  /// QoS2消息完成
  /// QoS 2 publish received, part 3
  /// </summary>
  [MessageType(MessageType.Pubcomp)]
  internal sealed class PublishCompMessage : MqttMessage
  {
    /// <summary>
    /// 消息ID
    /// </summary>
    public short MessageIdentifier { get; set; }

    protected override void Decode(Stream stream)
    {
      MessageIdentifier = stream.ReadShort();
    }
  }
}