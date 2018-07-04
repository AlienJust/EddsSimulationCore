using System.IO;

namespace nMqtt.Messages
{
    /// <summary>
    /// 发布消息
    /// </summary>
    [MessageType(MessageType.Publish)]
    internal sealed class PublishMessage : MqttMessage
    {
        /// <summary>
        /// 主题
        /// </summary>
        public string TopicName { get; set; }
        /// <summary>
        /// 报文标识符
        /// </summary>
        public short MessageIdentifier { get; set; }
        /// <summary>
        /// 有效载荷
        /// </summary>
        public byte[] Payload { get; set; }

        public override void Encode(Stream stream)
        {
            using (var body = new MemoryStream())
            {
                body.WriteString(TopicName);
                body.WriteShort(MessageIdentifier);
                body.Write(Payload, 0, Payload.Length);

                FixedHeader.RemaingLength = (int)body.Length;
                FixedHeader.WriteTo(stream);
                body.WriteTo(stream);
            }
        }

        protected override void Decode(Stream stream)
        {
            //variable header
            TopicName = stream.ReadString();
            if (FixedHeader.Qos == Qos.AtLeastOnce || FixedHeader.Qos == Qos.ExactlyOnce)
                MessageIdentifier = stream.ReadShort();

            //playload
            var len = FixedHeader.RemaingLength - (TopicName.Length + 2);
            Payload = new byte[len];
            stream.Read(Payload, 0, len);
        }
    }

    /// <summary>
    /// 发布回执
    /// QoS level = 1
    /// </summary>
    [MessageType(MessageType.Puback)]
    internal sealed class PublishAckMessage : MqttMessage
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

    /// <summary>
    /// QoS2消息回执
    /// QoS 2 publish received, part 1
    /// </summary>
    [MessageType(MessageType.Pubrec)]
    internal sealed class PublishRecMessage : MqttMessage
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

    /// <summary>
    /// QoS2消息释放
    /// QoS 2 publish received, part 2
    /// </summary>
    [MessageType(MessageType.Pubrel)]
    internal sealed class PublishRelMessage : MqttMessage
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
