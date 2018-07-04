namespace nMqtt.Messages
{
    /// <summary>
    /// 断开连接
    /// </summary>
    [MessageType(MessageType.Disconnect)]
    internal sealed class DisconnectMessage : MqttMessage
    {
    }
}
