using System;
using AJ.Std.Text;

namespace nMqtt {
    public class MessageReceivedEventArgs : EventArgs {
        public MessageReceivedEventArgs(string topic, byte[] data)
        {
            Topic = topic;
            Data = data;
        }

        public string Topic { get; }
        public byte[] Data { get; }

        public override string ToString() {
            return "Topic: " + Topic + ", received: " + Data.ToText();
        }
    }
}