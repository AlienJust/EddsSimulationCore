using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ScadaClient.Udp {
	public sealed class ReallyAsyncUdpClient : IScadaClient2 {
		private readonly UdpClient _client;
		private readonly ConcurrentDictionary<EndPoint, TaskCompletionSource<byte[]>> _tcsDictionary;

		public ReallyAsyncUdpClient(int localPort) {
			_client = new UdpClient(new IPEndPoint(IPAddress.Any, localPort));
			_tcsDictionary = new ConcurrentDictionary<EndPoint, TaskCompletionSource<byte[]>>();

			Task.Run(() => {
				IPEndPoint ipEndPoint = null;

				while (true) {
					try {
						var receivedBytes = _client.Receive(ref ipEndPoint);
						if (_tcsDictionary.TryGetValue(ipEndPoint, out var tcs)) tcs.SetResult(receivedBytes);
					}
					catch (SocketException) {
						;//при невозможности соединения продолжаем работать
					}

				}
			});
		}

		public async Task<byte[]> SendReceiveAsync(byte[] msg, string ip, int port, int timeOut) {
			var endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
			var tcs = new TaskCompletionSource<byte[]>();

			try {
				var tokenSource = new CancellationTokenSource(timeOut);
				var token = tokenSource.Token;
				if (!_tcsDictionary.ContainsKey(endPoint)) _tcsDictionary.TryAdd(endPoint, tcs);
				_client.Send(msg, msg.Length, ip, port);

				var result = await tcs.Task.WithCancellation(token);
				return result;
			}

			finally {
				_tcsDictionary.TryRemove(endPoint, out tcs);
			}
		}
	}

	public interface IScadaClient2 {
		Task<byte[]> SendReceiveAsync(byte[] msg, string ip, int port, int timeOut);
	}

	static class TaskExtension {
		public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken) {
			await task.ContinueWith(_ => { }, cancellationToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
			return await task;
		}
	}

	public interface IScadaClient3 {
		Task<Tuple<byte, byte[]>> SendMicroPacketAndWaitForCommand(ushort netAddress, byte linkLevel, int timeoutMs);
		Task<Tuple<byte, byte[]>> SendDataAndWaitForCommand(ushort netAddress, byte commandCode, byte[] data);
	}
}