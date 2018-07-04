using System.Net.Sockets;

namespace ScadaClient.Udp {
	sealed class SocketWithBufferAndAttemptsCount {
		public SocketWithBufferAndAttemptsCount(Socket socket, byte[] buffer, int attemptsCount) {
			B = buffer;
			S = socket;
			A = attemptsCount;
		}

		public Socket S { get; }

		public byte[] B { get; }

		public int A { get; }
	}
}