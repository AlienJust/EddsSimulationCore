namespace nMqtt.Messages {
  /// <summary>
  /// 断开连接
  /// </summary>
  [MessageType(MessageType.Disconnect)]
  public sealed class DisconnectMessage : MqttMessage { }
}