namespace nMqtt.Messages
{
    /// <summary>
    /// PING请求
    /// </summary>
    [MessageType(MessageType.Pingreq)]
    internal sealed class PingReqMessage : MqttMessage
    {
    }

    /// <summary>
    /// PING响应
    /// </summary>
    [MessageType(MessageType.Pingresp)]
    internal class PingRespMessage : MqttMessage
    {
    }
}
