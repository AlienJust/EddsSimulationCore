using System;
using ScadaClient.Contracts;

namespace PollServiceProxy {
	public interface INamedScadaLink {
		string Name { get; }
		IScadaClient Link { get; }
		event EventHandler<DataReceivedEventArgs> DataReceived;
		event EventHandler<DisconnectedEventArgs> Disconnected;
	}
}