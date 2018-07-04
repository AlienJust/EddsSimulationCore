using System;
using System.Linq;
using System.Text;
using AJ.Std.Loggers;
using AJ.Std.Loggers.Contracts;
using AJ.Std.Text;
using AJ.Std.Text.Contracts;
using Audience;
using BumizNetwork.Contracts;
using RJCP.IO.Ports;
using Timer = System.Timers.Timer;

namespace BumizNetwork.SerialChannel {

	/// <summary>
	/// Сильно связанный класс с BumizNetwork
	/// </summary>
	public sealed class SerialChannelSimple : IDisposable {
		private static readonly ILogger Log = new RelayMultiLogger(true, new RelayLogger(Env.GlobalLog, new ChainedFormatter(new ITextFormatter[] {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})), new RelayLogger(new ColoredConsoleLogger(ConsoleColor.DarkYellow, Console.BackgroundColor), new ChainedFormatter(new ITextFormatter[] {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})));
		private readonly int _openingTimeout;
		private readonly SerialPortStream _port;
		private Timer _timerCheckPortIsOpened;
		private bool _disposed;
		public ushort SNetId;


		public SerialChannelSimple(string portName, int baudRate, int openingTimeout) {
			Log.Log(".ctor called");
			_openingTimeout = openingTimeout;
			//var AJ.Std.Serial.SerialPortExtender
			Log.Log("The line before <_port = new SerialPort(portName)>");
			_port = new SerialPortStream(portName) {
				BaudRate = baudRate,
				Parity = Parity.None,
				DataBits = 8,
				StopBits = StopBits.One,
				ReadTimeout = int.MaxValue,
				WriteTimeout = 500,
			};
			Log.Log("The line after <_port = new SerialPort(portName)>");
			try {
				if (_port.IsOpen) _port.Close();
				OpenPort();
			}
			catch (Exception ex) {
				Dispose();
				Log.Log(ex.ToString());
				throw;
			}
			Log.Log("Ready to setup port_opened_watcher");
			SetUpPortOpenedWatcher();
			Log.Log(".ctor returns");
		}

		private void SetUpPortOpenedWatcher() {
			_timerCheckPortIsOpened = new Timer(1000) {AutoReset = true};
			_timerCheckPortIsOpened.Elapsed += delegate {
				if (!_disposed) {
					if (!_port.IsOpen) {
						Log.Log("Port is not opened, trying to open...");
						try {
							OpenPort();
						}
						catch (Exception ex) {
							Log.Log(ex.ToString());
						}
					}
				}
			};
			Log.Log(GetPortInfo());
			_timerCheckPortIsOpened.Start();
		}

		private void OpenPort() {
			Log.Log("Opening port...");
			_port.Open();
			Log.Log("Port was opened OK");
			//_port.ClearRtsControlToggle();
			//_port.SetRtsControlToggle();
			_port.DiscardInBuffer();
			_port.DiscardOutBuffer();
			SNetId = RetrieveSNetId(_openingTimeout);
			Log.Log("SNetId retreived");
		}

		private void CloseChannel() {
			_timerCheckPortIsOpened.Stop();
			if (_port.IsOpen)
				_port.Close();
			Log.Log("Последовательный канал закрыт");
		}

		private ushort RetrieveSNetId(int timeoutSeconds) {
			try {
				Log.Log("Попытка получить SNetID");
				var bytes = Encoding.ASCII.GetBytes("ATS05?\x0D");
				_port.WriteBytes(bytes, 0, bytes.Length);

				var inBytes = _port.ReadBytes(8, timeoutSeconds /*ReadTimeoutSafe*/);

				Log.Log("inBytes = " + inBytes.ToText());
				Log.Log("inBytes[2] = " + inBytes[2] + "  inBytes[2].XtoV = " + inBytes[2].XtoV());
				Log.Log("inBytes[3] = " + inBytes[3] + "  inBytes[3].XtoV = " + inBytes[3].XtoV());
				Log.Log("inBytes[4] = " + inBytes[4] + "  inBytes[4].XtoV = " + inBytes[4].XtoV());
				Log.Log("inBytes[5] = " + inBytes[5] + "  inBytes[5].XtoV = " + inBytes[5].XtoV());

				var result = (ushort) (inBytes[5].XtoV() + inBytes[4].XtoV()*0x10 + inBytes[3].XtoV()*0x100 + inBytes[2].XtoV()*0x1000);
				Log.Log("SNetId = " + result);
				return result;
			}
			catch (Exception ex) {
				Log.Log("Не удалось получить SNetId");
				throw new Exception("Не удалось получить SNetId", ex);
			}
		}

		public ushort ResolveDNetIdByBroadcastAtModemCommand(ObjectAddress address, ushort sNetId, int timeoutSeconds) {
			Log.Log("Попытка получить DNetID броадкастом");

			byte[] netbuf;
			switch (address.Way) {
				case NetIdRetrieveType.SerialNumber: {
					var buf = new byte[6];
					buf[0] = 0x03; // sn
					buf[1] = (byte) ((address.Value & 0x000000FF));
					buf[2] = (byte) ((address.Value & 0x0000FF00) >> 8);
					buf[3] = (byte) ((address.Value & 0x00FF0000) >> 16);
					buf[4] = (byte) (sNetId & 0x00FF);
					buf[5] = (byte) ((sNetId & 0xFF00) >> 8);
					netbuf = buf.GetNetBuffer(0x00FF, 0x09);
				}
					break;

				case NetIdRetrieveType.InteleconAddress: {
					var buf = new byte[5];
					buf[0] = 0x00; // ia
					buf[1] = (byte) ((address.Value & 0x00FF));
					buf[2] = (byte) ((address.Value & 0xFF00) >> 8);
					buf[3] = (byte) (sNetId & 0x00FF);
					buf[4] = (byte) ((sNetId & 0xFF00) >> 8);
					netbuf = buf.GetNetBuffer(0x00FF, 0x09);
				}
					break;

				case NetIdRetrieveType.OldProtocolSerialNumber: {
					var buf = new byte[5];
					buf[0] = 0x03; // sn
					buf[1] = (byte) (address.Value & 0x00FF);
					buf[2] = (byte) ((address.Value & 0xFF00) >> 8);
					buf[3] = (byte) (sNetId & 0x00FF);
					buf[4] = (byte) ((sNetId & 0xFF00) >> 8);

					netbuf = buf.GetNetBuffer(0x00FF, 0x09);
				}
					break;
				default:
					throw new Exception("Не поддерживаемый тип адресации");
			}
			//                                             отсюда число байт, которые ожидает модем
			var bytes = Encoding.ASCII.GetBytes("AT+BCASTB:" + netbuf.Length.ToString("x2").ToUpper() + ",00\x0D");
			Log.Log("Байты для отправки: " + bytes.ToText() + " = " + Encoding.ASCII.GetString(bytes));

			_port.WriteBytes(bytes, 0, bytes.Length);
			//Thread.Sleep(100);

			Log.Log("В порту будет отправлено: " + netbuf.ToText());
			_port.WriteBytes(netbuf, 0, netbuf.Length);
			var inBytes = _port.ReadBytes(10, /*ReadTimeoutSafe*/timeoutSeconds);
			Log.Log("Из порта получены байты: " + inBytes.ToText());

			var dNetId = (ushort) (inBytes[5] + (inBytes[6] << 8));
			Log.Log("Получен DNetId = 0x" + dNetId.ToString("X4") + " = " + dNetId + " dec");
			return dNetId;
		}

		public byte[] RequestSomething(ushort dNetId, byte[] buffer, int waitingBytesCount, int timeoutSeconds) {
			SetDNetIdByUnicastAtModemCommand(dNetId, (ushort) buffer.Length);
			Log.Log("Буфер_отправления[" + buffer.Length + "] = " + buffer.ToText() + "    Таймаут = " + timeoutSeconds + "    Ожидаемое число байт ответа = " + waitingBytesCount);

			_port.WriteBytes(buffer, 0, buffer.Length);
			var reply = _port.ReadBytes(waitingBytesCount, timeoutSeconds);

			Log.Log("Получен ответ: " + reply.ToText());
			return reply;
		}

		public byte[] SendAndReceiveInteleconCommand(ushort dNetId, ushort? netAddress, byte[] buffer, int timeoutSeconds) {
			Log.Log("Обмен по протоколу Интелекон c предварительным юникаст-запросом");
			SetDNetIdByUnicastAtModemCommand(dNetId, (ushort) buffer.Length);

			Log.Log("Запрос[" + buffer.Length + "] = " + buffer.ToText() + "    Таймаут = " + timeoutSeconds + " сек.");

			_port.DiscardInBuffer();
			_port.DiscardOutBuffer();
			_port.WriteBytes(buffer, 0, buffer.Length);
			var reply = _port.ReadInteleconCommand(netAddress, timeoutSeconds);
			Log.Log("Получен ответ: " + reply.ToText());
			return reply.ToArray();
		}

		public byte[] RequestBytes(byte[] outData, int? waitBytesCount, int timeoutSeconds) {
			Log.Log("sending: " + outData.Aggregate(string.Empty, (current, b) => current + (char) b));
			_port.WriteBytes(outData, 0, outData.Length);
			if (!waitBytesCount.HasValue) return null;

			return _port.ReadBytes(waitBytesCount.Value, timeoutSeconds);
		}

		//private const int PauseAfterDnetIdResolve = 200;

		public void SetDNetIdByUnicastAtModemCommand(ushort dNetId, ushort queryLength) {

			var bytes = dNetId.GetBufferSetDNetId(queryLength);
			Log.Log("Установка DNetID, будет отправлен запрос: " + bytes.ToText() + " = " + Encoding.ASCII.GetString(bytes));
			_port.WriteBytes(bytes, 0, bytes.Length);
			Log.Log("DNetID установлен в значение 0x" + dNetId.ToString("X4"));// + ", пауза " + PauseAfterDnetIdResolve + " мс");
			//Thread.Sleep(PauseAfterDnetIdResolve);
		}



		public string GetPortInfo() {
			var result = Environment.NewLine;
			try {
				result += "PortName = " + _port.PortName + Environment.NewLine;
				result += "BaudRate = " + _port.BaudRate + Environment.NewLine;
			}
			catch (Exception ex) {
				result += ex;
			}
			return result;
		}



		public void Dispose() {
			if (!_disposed) {
				CloseChannel();
				_disposed = true;
			}
		}
	}
}
