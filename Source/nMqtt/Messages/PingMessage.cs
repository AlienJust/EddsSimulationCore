namespace nMqtt.Messages {
  /// <summary>
  /// PING请求
  /// </summary>
  [MessageType(MessageType.Pingreq)]
  public sealed class PingReqMessage : MqttMessage { }
}