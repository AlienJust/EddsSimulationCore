using System;
using System.Collections.Generic;
using AJ.Std.Loggers;
using AJ.Std.Loggers.Contracts;
using AJ.Std.Text;
using AJ.Std.Text.Contracts;
using Audience;
using BumizNetwork.Contracts;
using Commands.Contracts;

namespace BumizNetwork.Shared {
	public static class MonoChannelExtensions {
		private static readonly ILogger Log = new RelayMultiLogger(true, new RelayLogger(Env.GlobalLog, new ChainedFormatter(new ITextFormatter[] { new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ") })), new RelayLogger(new ColoredConsoleLogger(ConsoleColor.DarkYellow, Console.BackgroundColor), new ChainedFormatter(new ITextFormatter[] { new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ") })));

		public static void SendInteleconCommandAsync(this IMonoChannel channel, IInteleconCommand command, ObjectAddress objectAddress, int timeout, Action<ISendResultWithAddress> onComplete) {
			SendInteleconCommandAsync(channel, command, objectAddress, timeout, onComplete, IoPriority.Normal);
		}

		public static void SendInteleconCommandAsync(this IMonoChannel channel, IInteleconCommand command, ObjectAddress objectAddress, int timeout, Action<ISendResultWithAddress> onComplete, IoPriority priority) {
			var sendingItem = new AddressedSendingItem {
				Address = objectAddress,
				WaitTimeout = timeout,
				AttemptsCount = 1, // Всё верно, команда Интелекон упаковывается в посылку, размер которой на 8 байт больше этой Интелекон команды

				// 2013.07.26 - Рома сказал, что 0x00FF вместо адреса применяется при работе с пульта
				// 2013.08.15 - Точнее при работе с SerialNumber
				Buffer = command.Serialize().GetNetBuffer((ushort)(objectAddress.Way == NetIdRetrieveType.SerialNumber || objectAddress.Way == NetIdRetrieveType.OldProtocolSerialNumber ? 0x00FF : objectAddress.Value), command.Code)
			};
			channel.AddCommandToQueueAndExecuteAsync(
				new QueueItem {
					SendingItems = new List<IAddressedSendingItem> { sendingItem },
					OnComplete = results => {
						if (results == null) {
							onComplete(new SendingResultWithAddress(null, new Exception("Список ответов не существует (is null)"), null, 0));
						}
						else if (results.Count == 1) {
							var bytes = results[0].Bytes;
							var externalException = results[0].ChannelException;
							if (externalException == null) {
								Log.Log("Все байты: " + bytes.ToText());
								Exception internalException = null;
								byte[] infoBytes = null;
								ushort addressInReply = 0;
								try {
									// Возвращаемый код передаётся в третьем байте буфера
									bytes.CheckInteleconNetBufCorrect((byte)(sendingItem.Buffer[2] + 10), null);
									addressInReply = (ushort)(bytes[3] * 0x100 + bytes[4]);
									Log.Log("Адрес ответчика: " + addressInReply + " или 0x" + addressInReply.ToString("X4"));
									infoBytes = bytes.GetInteleconInfoReplyBytes();
									Log.Log("Байты информационного поля: " + infoBytes.ToText());
								}
								catch (Exception ex) {
									Log.Log(ex.ToString());
									internalException = ex;
								}
								finally {
									onComplete(new SendingResultWithAddress(infoBytes, internalException, results[0].Request, addressInReply));
								}
							}
							else onComplete(new SendingResultWithAddress(null, externalException, results[0].Request, 0));
						}
						else onComplete(new SendingResultWithAddress(null, new Exception("Неверное количество ответов: " + results.Count + " (ожидался один ответ)"), null, 0)); // тут даже не определить, какой объект послал исключение, надеюсь, никогда не выскочит
					}
				}, priority);
		}


		public static void SendInteleconCommandToManyProgressive(this IMonoChannel channel, IInteleconCommand command, List<ObjectAddress> objects, int timeout, Action<ISendResultWithAddress> onEachComplete) {
			foreach (var objectAddress in objects) {
				channel.SendInteleconCommandAsync(command, objectAddress, timeout, onEachComplete);
			}
		}


		public static void SendManyInteleconCommandsProgressive(this IMonoChannel channel, List<IInteleconCommand> commands, ObjectAddress objectAddress, int timeout, Action<ISendResultWithAddress> onEachComplete) {
			foreach (var command in commands) {
				channel.SendInteleconCommandAsync(command, objectAddress, timeout, onEachComplete);
			}

		}
	}
}