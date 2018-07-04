using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using AJ.Std.Loggers;
using AJ.Std.Loggers.Contracts;
using AJ.Std.Text;
using AJ.Std.Text.Contracts;
using Audience;
using RJCP.IO.Ports;

namespace BumizNetwork.SerialChannel
{
	// TODO: Use library AlienJust.Support.SerialPort
	static class SerialChannelExtensions
	{
		private static readonly ILogger Log = new RelayMultiLogger(true, new RelayLogger(Env.GlobalLog, new ChainedFormatter(new ITextFormatter[] { new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ") })), new RelayLogger(new ColoredConsoleLogger(ConsoleColor.DarkYellow, Console.BackgroundColor), new ChainedFormatter(new ITextFormatter[] { new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ") })));
		public static void WriteBytes(this SerialPortStream port, byte[] bytes, int offset, int count)
		{
			port.DiscardOutBuffer();
			Log.Log("Удаление всех данных исходящего буфера последовательного порта...");
			var discardedInBytes = port.ReadAllBytes();
			Log.Log("Удалены следующие байты: " + discardedInBytes.ToText());
			port.Write(bytes, 0, bytes.Length);
		}

		private static readonly Stopwatch ReadEplasedTimer = new Stopwatch();

		public static byte[] ReadBytes(this SerialPortStream port, int bytesCount, int timeoutInSeconds) {
			var inBytes = new byte[bytesCount];
			int totalReadedBytesCount = 0;

			const int iterationsPerSecondCount = 20;
			TimeSpan maximumMillsecondsCountToWaitAfterEachIteration = TimeSpan.FromMilliseconds(1000.0/iterationsPerSecondCount);
			Log.Log("Iteration period = " + maximumMillsecondsCountToWaitAfterEachIteration.TotalMilliseconds.ToString("f2") + " ms");
			//int iterationsLeft = timeoutInSeconds * iterationsPerSecondCount; // check each X ms

			for (int i = 0; i < timeoutInSeconds; ++i) {
				for (int j = 0; j < iterationsPerSecondCount; ++j) {
					ReadEplasedTimer.Restart();

					var bytesToRead = port.BytesToRead;

					if (bytesToRead != 0) {
						var currentReadedBytesCount = port.Read(inBytes, totalReadedBytesCount, bytesCount - totalReadedBytesCount);
						Log.Log("Incoming bytes now are = " + inBytes.ToText());
						totalReadedBytesCount += currentReadedBytesCount;
						Log.Log("Total readed bytes count=" + totalReadedBytesCount);
						Log.Log("Current readed bytes count=" + currentReadedBytesCount);

						if (totalReadedBytesCount == inBytes.Length) {
							Log.Log("Result incoming bytes are = " + inBytes.ToText());
							Log.Log("Discarding remaining bytes...");
							Log.Log("Discarded bytes are: " + port.ReadAllBytes().ToText());
							return inBytes;
						}
					}
					ReadEplasedTimer.Stop();
					//Log.Log("Iteration operation time = " + ReadEplasedTimer.Elapsed.TotalMilliseconds.ToString("f2") + " ms");
					var sleepTime = maximumMillsecondsCountToWaitAfterEachIteration - ReadEplasedTimer.Elapsed;
					if (sleepTime.TotalMilliseconds > 0) Thread.Sleep(sleepTime);
				}
			}

			Log.Log("Timeout, dropping all bytes...");
			Log.Log("Discarded bytes are: " + port.ReadAllBytes().ToText());
			Log.Log("Rising timeout exception now");
			throw new TimeoutException("ReadFromPort timeout");
		}

		public static List<byte> ReadInteleconCommand(this SerialPortStream port, ushort? netAddress, int timeoutInSeconds)
		{
			var inBuffer = new byte[512];
			int readedCount = 0;

			const int iterationsPerSecondCount = 20;
			TimeSpan maximumMillsecondsCountToWaitAfterEachIteration = TimeSpan.FromMilliseconds(1000.0 / iterationsPerSecondCount);
			int iterationsLeft = timeoutInSeconds * iterationsPerSecondCount; // check each X ms
			
			for (int i = iterationsLeft; i > 0; --i)
			{
				ReadEplasedTimer.Restart();
				
				// Обязательно проводить проверку, иначе метод port.Read зависнет
				var bytesToRead = port.BytesToRead;
				if (bytesToRead != 0) {
					var readedBytesInCurrentIteration = port.Read(inBuffer, readedCount, bytesToRead);
					readedCount += readedBytesInCurrentIteration;
				}
				// Check buffer for commands:
				if (readedCount >= 8) {
					for (int x = readedCount - 8; x >= 0; --x) {
						var possibleStart = inBuffer[x];
						if (possibleStart == 0x7A) {
							// Possible cmd start!
							var len = inBuffer[x + 1];
							if (len <= readedCount - x - 4) {
								var possibleCmd = new byte[len + 4];
								for (int y = x; y < x + len + 4; ++y) {
									possibleCmd[y - x] = inBuffer[y];
								}
								// Analyze CMD:
								try {
									possibleCmd.CheckInteleconNetBufCorrect(null, netAddress);
									return possibleCmd.ToList();
								}
								catch {
									continue;
								}
							}
						}
					}
				}
				ReadEplasedTimer.Stop();
				//Log.Log("Iteration operation time = " + ReadEplasedTimer.Elapsed.TotalMilliseconds.ToString("f2") + " ms");
				var sleepTime = maximumMillsecondsCountToWaitAfterEachIteration - ReadEplasedTimer.Elapsed;
				if (sleepTime.TotalMilliseconds > 0) Thread.Sleep(sleepTime);
			}

			Log.Log("Истекло время ожидания ответа, будет произведено удаление всех принятых данных из буфера последовательного канала...");
			Log.Log("Удалены следующие байты: " + port.ReadAllBytes().ToText() + ", выброс исключения по таймауту");
			throw new TimeoutException("Истекло время ожидания ответа");
		}

		public static byte[] ReadAllBytes(this SerialPortStream port)
		{
			var bytesToRead = port.BytesToRead;
			var result = new byte[bytesToRead];
			port.Read(result, 0, bytesToRead);
			return result;
		}
	}
}
