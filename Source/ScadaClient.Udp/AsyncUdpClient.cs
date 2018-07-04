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
	public class AsyncUdpClient : IScadaClient {
		readonly int _connectionDropTimeMs;

		static readonly ILogger Log = new RelayMultiLogger(true,
			new RelayLogger(Env.GlobalLog, new ChainedFormatter(new ITextFormatter[] { new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ") })),
			new RelayLogger(new ColoredConsoleLogger(ConsoleColor.Magenta, Console.BackgroundColor), new ChainedFormatter(new ITextFormatter[] { new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ") })));

		readonly object _connectionSyncObj = new object();
		readonly object _disconnectionSyncObj = new object();
		readonly object _socketSyncObj = new object();

		readonly IWorker<Action> _notifyThreadWorker;
		readonly Thread _dropConnectionWachdog; // TODO: stop thread

		readonly string _targetIp;
		readonly int _targetPort;
		readonly IPEndPoint _targetEndpoint;

		private readonly int _localPort;

		bool _isConnecting;
		bool _isDisconnecting;
		Socket _localUdpSocket;
		private readonly WaitableCounter _connectionCounter;

		public AsyncUdpClient(string targetIp, int targetPort, int localPort, int connectionDropPeriodMs) {
			_targetIp = targetIp;
			_targetPort = targetPort;
			_targetEndpoint = new IPEndPoint(IPAddress.Parse(_targetIp), _targetPort);

			_localPort = localPort;
			_connectionDropTimeMs = connectionDropPeriodMs;
			_connectionCounter = new WaitableCounter();

			_notifyThreadWorker = new SingleThreadedRelayQueueWorkerProceedAllItemsBeforeStopNoLog<Action>("AsyncUDPClientBackThread", a => a(), ThreadPriority.Normal, true, null); // Нотификатор получения данных

			EstablishNewConnection();

			_dropConnectionWachdog = new Thread(WachForConnectionDropFunc) { IsBackground = true, Priority = ThreadPriority.BelowNormal, Name = "ConnectionDropWachdog" };
			_dropConnectionWachdog.Start();
		}

		void WachForConnectionDropFunc() {
			while (true) {
				Thread.Sleep(_connectionDropTimeMs);
				Disconnect(_localUdpSocket, new Exception("Подключение сброшено преднамеренно (период сброса = " + _connectionDropTimeMs + "мс)"));
				EstablishNewConnection();
			}
		}

		public void EstablishNewConnection() {
			Logg("EstablishNewConnection > Число подключений не считая этого = " + _connectionCounter.Count);
			lock (_connectionSyncObj) {
				if (!_isConnecting)
					_isConnecting = true;
				else {
					Logg("Подключение уже выполняется, отмена");
					return; // уже идет подключение, другие попытки параллельного подключения отбрасываются
				}
			}

			try {
				Logg("Подключение...");


				if (_localUdpSocket == null) {
					_localUdpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
					_localUdpSocket.Bind(new IPEndPoint(IPAddress.Any, _localPort));
				}

				_localUdpSocket.BeginConnect(_targetEndpoint, ConnectCallback, _localUdpSocket); // Сокет передается, чтобы не заморачиваться с проверкой, тот ли это самый объект
				_connectionCounter.IncrementCount();
			}
			catch (Exception ex) {
				Logg("Ошибка при установке подключения, исключение: " + ex);
				Disconnect(_localUdpSocket, ex);
				lock (_connectionSyncObj) {
					_isConnecting = false;
				}
				//EstablishNewConnection(); // Can rise stack overflow
			}
		}

		void ConnectCallback(IAsyncResult ar) {
			var localSocket = (Socket)ar.AsyncState; // Гарант того, что сокет существует
			try {
				localSocket.EndConnect(ar); // сообщаем, что асинхронная операция завершена
				var socketWithBuffer = new SocketWithBuffer(localSocket, new byte[1024]); // у каждого нового сокета свой буфер
				localSocket.BeginReceive(socketWithBuffer.B, 0, socketWithBuffer.B.Length, SocketFlags.None, ReceivedCallback, socketWithBuffer);

				lock (_connectionSyncObj) {
					_isConnecting = false;
				}
			}
			catch (Exception ex) {
				Logg("Ошибка при завершении подключения, либо ошибка при начале ожидания данных: " + ex);
				Disconnect(localSocket, ex);

				lock (_connectionSyncObj) {
					_isConnecting = false;
				}
				//EstablishNewConnection(); // Can rise stack overflow
			}
		}

		void ReceivedCallback(IAsyncResult ar) {
			var socketWithBuffer = (SocketWithBuffer)ar.AsyncState;
			try {
				int bytesToRead = socketWithBuffer.S.EndReceive(ar); // сообщаем, что операция подсоединения завершена

				var data = new byte[bytesToRead];
				Array.Copy(socketWithBuffer.B, data, bytesToRead);
				try {
					socketWithBuffer.S.BeginReceive(socketWithBuffer.B, 0, socketWithBuffer.B.Length, SocketFlags.None, ReceivedCallback, socketWithBuffer);
				}
				catch (Exception ex) {
					throw new Exception("Не удалось инициировать новый прием данных", ex);
				}

				// обработка данных в отдельном потоке:
				_notifyThreadWorker.AddWork(
					() => {
						try {
							if (bytesToRead >= 8) {
								if (DataReceived != null) {
									var netAddr = (ushort)(data[4] + (data[3] << 8));
									var cmdCode = data[2];
									var rcvData = new byte[bytesToRead - 8];
									for (int i = 0; i < rcvData.Length; ++i) {
										rcvData[i] = data[i + 5];
									}
									Logg("Данные в формате Интелекон успешно получены, netAddr=" + netAddr + " cmdCode=" + cmdCode + " info=" + rcvData.ToText() + ", оповещаем подписчиков");
									DataReceived.SafeInvoke(this, new DataReceivedEventArgs(netAddr, cmdCode, rcvData)); // Вызывается не в основном потоке!
								}
							}
						}
						catch (Exception ex) {
							Logg("Ошибка при обработке данных, исключение: " + ex);
						}
					});
			}
			catch (Exception ex) {
				Logg("Ошибка при завершении приема данных или при начале нового приема: " + ex);
				Disconnect(socketWithBuffer.S, ex);
				EstablishNewConnection();
			}
		}

		public void Disconnect(Socket localSocket, Exception reason) {
			lock (_disconnectionSyncObj) {
				if (!_isDisconnecting)
					_isDisconnecting = true;
				else {
					Logg("Отключение уже выполняется, отмена");
					return; // уже идет подключение, другие попытки параллельного подключения отбрасываются
				}
			}

			Logg("Disconnect > до начала отключения число подключений = " + _connectionCounter.Count);
			try {
				Logg("Отключение по причине: " + reason);
				localSocket.Disconnect(true);
				Logg("Отключено");
			}
			catch (Exception ex) {
				Logg("Ошибка при отключении" + ex);
				lock (_disconnectionSyncObj) {
					_isDisconnecting = false;
				}
			}

			finally {
				_connectionCounter.DecrementCount();
				_notifyThreadWorker.AddWork(() => Disconnected.SafeInvoke(this, new DisconnectedEventArgs(reason)));
			}
		}

		public void SendData(ushort netAddress, byte commandCode, byte[] data) {
			var buffer = data.GetNetBuffer(netAddress, commandCode);
			Logg("Будут отправлены следующие байты данных: " + buffer.ToText());
			SendAsync(buffer);
		}

		void SendAsync(byte[] data) {
			Socket localSocket;
			lock (_socketSyncObj) {
				localSocket = _localUdpSocket;
			}

			try {
				localSocket.BeginSend(data, 0, data.Length, SocketFlags.None, SendCallback, localSocket);
			}
			catch (Exception ex) {
				Logg("Не удалось начать отправку данных, исключение: " + ex);
				Disconnect(localSocket, ex);
				EstablishNewConnection(); // TODO: with tail of again sending
			}
		}

		void SendCallback(IAsyncResult ar) {
			var localSocket = (Socket)ar.AsyncState; // TODO: Be more carefull
			try {
				localSocket.EndSend(ar);
				Logg("Данные отправлены");
			}
			catch (Exception ex) {
				Logg("Не удалось завершить отправку данных" + ex);
				Disconnect(localSocket, ex);
				EstablishNewConnection(); // TODO: with tail of again sending
			}
		}

		public void SendMicroPacket(ushort netAddress, byte linkLevel) {
			var netAddrB1 = (byte)((netAddress & 0xFF00) >> 8);
			var netAddrB0 = (byte)(netAddress & 0x00FF);
			var buf = new byte[4];
			buf[0] = 0x71;
			buf[1] = netAddrB1;
			buf[2] = netAddrB0;
			buf[3] = linkLevel;
			Logg("Отправка микропакета: " + buf.ToText());
			SendAsync(buf);
		}

		void Logg(object logObj) {
			Log.Log(_targetIp + ":" + _targetPort + " > " + logObj);
		}


		public event EventHandler<DataReceivedEventArgs> DataReceived;


		public event EventHandler<DisconnectedEventArgs> Disconnected;
	}

	public class AsyncQueuedUdpSender : IScadaClient {
		public void SendData(ushort netAddress, byte commandCode, byte[] data)
		{
			throw new NotImplementedException();
		}

		public void SendMicroPacket(ushort netAddress, byte linkLevel)
		{
			throw new NotImplementedException();
		}

		public event EventHandler<DataReceivedEventArgs> DataReceived;
		public event EventHandler<DisconnectedEventArgs> Disconnected;
	}
}