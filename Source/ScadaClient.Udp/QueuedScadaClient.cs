using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using AJ.Std.Concurrent;
using AJ.Std.Concurrent.Contracts;
using AJ.Std.Loggers;
using AJ.Std.Loggers.Contracts;
using AJ.Std.Text;
using AJ.Std.Text.Contracts;
using Audience;
using ScadaClient.Contracts;

namespace ScadaClient.Udp {
	public sealed class QueuedScadaClient : IScadaClient {
		private readonly IPEndPoint _serverEndPoint;
		private readonly UdpClient _client;
		private readonly IWorker<Action> _receiveWorker;
		private readonly IWorker<Action> _proceedReceivedDataWorker;
		private readonly IWorker<Action> _sendDataWorker;

		static readonly ILogger Log = new RelayMultiLogger(true, new RelayLogger(Env.GlobalLog, new ChainedFormatter(new ITextFormatter[] {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})), new RelayLogger(new ColoredConsoleLogger(ConsoleColor.Magenta, Console.BackgroundColor), new ChainedFormatter(new ITextFormatter[] {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})));

		public QueuedScadaClient(IPAddress serverIpAddress, int serverPort, int localPort) {
			_serverEndPoint = new IPEndPoint(serverIpAddress, serverPort);
			_client = new UdpClient(new IPEndPoint(IPAddress.Any, localPort));

			_receiveWorker = new SingleThreadedRelayQueueWorkerProceedAllItemsBeforeStopNoLog<Action>("ScadaUdpClientReceiveWorker", a => a(), ThreadPriority.BelowNormal, true, null);
			_receiveWorker.AddWork(DoReceiveInBackThread);

			_proceedReceivedDataWorker = new SingleThreadedRelayQueueWorkerProceedAllItemsBeforeStopNoLog<Action>("ScadaUdpClientProceedReceivedDataWorker", a => a(), ThreadPriority.Normal, true, null);
			_sendDataWorker = new SingleThreadedRelayQueueWorkerProceedAllItemsBeforeStopNoLog<Action>("ScadaUdpClientSendDataWorker", a => a(), ThreadPriority.Normal, true, null);
		}

		private void DoReceiveInBackThread() {
			// TODO: stop receive cycle
			while (true) {
				try {
					IPEndPoint remotePoint = null;
					var receivedData = _client.Receive(ref remotePoint); // receive thread waits here
					_proceedReceivedDataWorker.AddWork(() => {
						// PROCEED DATA THREAD:
						Log.Log("Something received from: " + remotePoint + " data: " + receivedData.ToText());
						if (Equals(remotePoint, _serverEndPoint)) {
							Log.Log("Remote point is correct");
							if (receivedData.Length >= 8) {
								var netAddr = (ushort) (receivedData[4] + (receivedData[3] << 8));
								var cmdCode = receivedData[2];
								var rcvData = new byte[receivedData.Length - 8];
								for (int i = 0; i < rcvData.Length; ++i) {
									rcvData[i] = receivedData[i + 5];
								}

								Log.Log("Invoked data received event");
								DataReceived?.Invoke(this, new DataReceivedEventArgs(netAddr, cmdCode, rcvData));
							}
						}
					});
				}
				catch (Exception e) {
					Console.WriteLine(e);
					//throw;
				}
			}
		}

		public void SendData(ushort netAddress, byte commandCode, byte[] data) {
			_sendDataWorker.AddWork(() => {
				var buffer = data.GetNetBuffer(netAddress, commandCode);
				_client.Send(buffer, buffer.Length, _serverEndPoint); // no timeout, no chance to know wether data were transmitted
			});
		}

		public void SendMicroPacket(ushort netAddress, byte linkLevel) {
			_sendDataWorker.AddWork(() => {
				// TODO: suppport networkaddress < 255
				var netAddrB1 = (byte) ((netAddress & 0xFF00) >> 8);
				var netAddrB0 = (byte) (netAddress & 0x00FF);
				var buffer = new byte[4];
				buffer[0] = 0x71;
				buffer[1] = netAddrB1;
				buffer[2] = netAddrB0;
				buffer[3] = linkLevel;
				_client.Send(buffer, buffer.Length, _serverEndPoint); // no timeout, no chance to know wether data were transmitted, yeah its UDP ><
			});
		}

		public event EventHandler<DataReceivedEventArgs> DataReceived;
		public event EventHandler<DisconnectedEventArgs> Disconnected;
	}
}