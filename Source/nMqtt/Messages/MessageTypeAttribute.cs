using System;

namespace nMqtt.Messages {
  /// <summary>
  /// 报文类型
  /// </summary>
  [AttributeUsage(AttributeTargets.Class)]
  public class MessageTypeAttribute : Attribute
  {
    public MessageTypeAttribute(MessageType messageType)
    {
      MessageType = messageType;
    }

    /// <summary>
    /// MQTT mesage type
    /// </summary>
    public MessageType MessageType { get; }
  }
}