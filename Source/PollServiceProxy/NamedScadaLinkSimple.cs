using System;
using ScadaClient.Contracts;
using ScadaClient.Udp;

namespace PollServiceProxy
{
    /// <summary>
    /// Link to SCADA with name simple release
    /// </summary>
    internal class NamedScadaLinkSimple : INamedScadaLink
    {
        private readonly IScadaClient _link;

        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler<DisconnectedEventArgs> Disconnected;

        public NamedScadaLinkSimple(string name, IScadaClient link)
        {
            Name = name;
            _link = link ?? throw new NullReferenceException(nameof(link));
            _link.DataReceived += LinkOnDataReceived;
            _link.Disconnected += LinkOnDisconnected;
        }

        private void LinkOnDisconnected(object sender, DisconnectedEventArgs eventArgs)
        {
            Disconnected.SafeInvoke(this, eventArgs);
        }

        private void LinkOnDataReceived(object sender, DataReceivedEventArgs eventArgs)
        {
            DataReceived.SafeInvoke(this, eventArgs);
        }


        public string Name { get; }

        public IScadaClient Link => _link;
    }
}