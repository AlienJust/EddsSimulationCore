using System;

namespace ScadaClient.Contracts {
	public sealed class DataReceivedEventArgs : EventArgs {
		public DataReceivedEventArgs(ushort netAddress, byte commandCode, byte[] data) {
			NetAddress = netAddress;
			CommandCode = commandCode;
			Data = data;
		}

		public byte[] Data { get; }

		public byte CommandCode { get; }

		public ushort NetAddress { get; }
	}
}