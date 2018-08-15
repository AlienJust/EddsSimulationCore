using System.IO;
using System.Collections.Generic;

namespace nMqtt.Messages
{
    /// <summary>
    /// Subcribe message
    /// </summary>
    [MessageType(MessageType.Subscribe)]
    public sealed class SubscribeMessage : MqttMessage
    {
        /// <summary>
        /// List of the topics to subscribe
        /// </summary>
        private readonly List<TopicAndQos> _topics = new List<TopicAndQos>();
        
        /// <summary>
        /// Message ID
        /// </summary>
        public short MessageIdentifier { get; set; }

        public override void Encode(Stream stream)
        {
            using (var body = new MemoryStream())
            {
                body.WriteShort(MessageIdentifier);

                foreach (var item in _topics)
                {
                    body.WriteString(item.Topic);
                    body.WriteByte((byte)item.Qos);
                }

                // MQTT 3.1.1 spec
                //Bits 3,2,1 and 0 of the fixed header of the SUBSCRIBE Control Packet are reserved and MUST be set to 
                // 0,0,1 and 0 respectively. The Server MUST treat any other value as malformed and close the Network Connection
                
                FixedHeader.MessageType = MessageType.Subscribe;
                FixedHeader.Dup = false;
                FixedHeader.Qos = Qos.AtLeastOnce;
                FixedHeader.Retain = false;
                
                FixedHeader.RemaingLength = (int)body.Length;
                FixedHeader.WriteTo(stream); 
                body.WriteTo(stream);         
            }
        }

        public void Subscribe(string topic, Qos qos)
        {
            _topics.Add(new TopicAndQos
            {
                Topic = topic,
                Qos = qos,
            });
        }

       
    }
    internal struct TopicAndQos
    {
        public string Topic { get; set; }
        public Qos Qos { get; set; }
    }
}