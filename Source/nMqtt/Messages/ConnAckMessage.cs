using System.IO;

namespace nMqtt.Messages {
  /// <summary>
  /// 连接回执
  /// </summary>
  [MessageType(MessageType.Connack)]
  public sealed class ConnAckMessage : MqttMessage {
    /// <summary>
    /// 当前会话
    /// </summary>
    public bool SessionPresent { get; set; }

    /// <summary>
    /// 连接返回码
    /// </summary>
    public ConnectReturnCode ConnectReturnCode { get; set; }

    protected override void Decode(Stream stream) {
      SessionPresent = (stream.ReadByte() & 0x01) == 1;
      ConnectReturnCode = (ConnectReturnCode) stream.ReadByte();
    }
  }
}