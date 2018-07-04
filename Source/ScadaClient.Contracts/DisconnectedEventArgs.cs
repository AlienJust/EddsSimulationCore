using System;

namespace ScadaClient.Contracts {
	public sealed class DisconnectedEventArgs : EventArgs {
		public Exception Reason { get; }

		public DisconnectedEventArgs(Exception reason) {
			Reason = reason;
		}
	}
}