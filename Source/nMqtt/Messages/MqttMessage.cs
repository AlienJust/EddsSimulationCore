using System;
using System.IO;

namespace nMqtt.Messages {
  /// <summary>
  /// MQTT Message (base class)
  /// </summary>
  internal abstract class MqttMessage {
    /// <summary>
    /// Header
    /// </summary>
    public FixedHeader FixedHeader { get; private set; }

    protected MqttMessage() {
      var att = (MessageTypeAttribute) Attribute.GetCustomAttribute(GetType(), typeof(MessageTypeAttribute));
      FixedHeader = new FixedHeader(att.MessageType);
    }

    protected MqttMessage(MessageType msgType) => FixedHeader = new FixedHeader(msgType);

    public virtual void Encode(Stream stream) => FixedHeader.WriteTo(stream);

    protected virtual void Decode(Stream stream) { }

    public static MqttMessage DecodeMessage(byte[] buffer) {
      using (var stream = new MemoryStream(buffer)) {
        return DecodeMessage(stream);
      }
    }


    private static MqttMessage DecodeMessage(Stream stream) {
      var header = new FixedHeader(stream);
      var msg = CreateMessage(header.MessageType);
      msg.FixedHeader = header;
      msg.Decode(stream);
      return msg;
    }

    private static MqttMessage CreateMessage(MessageType msgType) {
      switch (msgType) {
        case MessageType.Connect:
          return new ConnectMessage();
        case MessageType.Connack:
          return new ConnAckMessage();
        case MessageType.Disconnect:
          return new DisconnectMessage();
        case MessageType.Pingreq:
          return new PingReqMessage();
        case MessageType.Pingresp:
          return new PingRespMessage();
        case MessageType.Puback:
          return new PublishAckMessage();
        case MessageType.Pubcomp:
          return new PublishCompMessage();
        case MessageType.Publish:
          return new PublishMessage();
        case MessageType.Pubrec:
          return new PublishRecMessage();
        case MessageType.Pubrel:
          return new PublishRelMessage();
        case MessageType.Subscribe:
          return new SubscribeMessage();
        case MessageType.Suback:
          return new SubscribeAckMessage();
        case MessageType.Unsubscribe:
          return new UnsubscribeMessage();
        case MessageType.Unsuback:
          return new UnsubscribeMessage();
        default:
          throw new Exception("Unsupported Message Type");
      }
    }
  }
}