using System.Net.Sockets;

namespace ScadaClient.Udp {
  sealed class SocketWithBuffer {
    public SocketWithBuffer(Socket socket, byte[] buffer) {
      B = buffer;
      S = socket;
    }

    public Socket S { get; }

    public byte[] B { get; }
  }
}