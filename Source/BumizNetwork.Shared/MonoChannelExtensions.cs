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
    private static readonly ILogger Log = new RelayMultiLogger(true,
      new RelayLogger(Env.GlobalLog,
        new ChainedFormatter(new ITextFormatter[]
          {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})),
      new RelayLogger(new ColoredConsoleLogger(ConsoleColor.DarkYellow, Console.BackgroundColor),
        new ChainedFormatter(new ITextFormatter[]
          {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})));

    public static void SendInteleconCommandAsync(this IMonoChannel channel, IInteleconCommand command,
      ObjectAddress objectAddress, int timeout, Action<ISendResultWithAddress> onComplete) {
      SendInteleconCommandAsync(channel, command, objectAddress, timeout, onComplete, IoPriority.Normal);
    }

    public static void SendInteleconCommandAsync(this IMonoChannel channel, IInteleconCommand command,
      ObjectAddress objectAddress, int timeout, Action<ISendResultWithAddress> onComplete, IoPriority priority) {
      var sendingItem = new AddressedSendingItem {
        Address = objectAddress,
        WaitTimeout = timeout,
        AttemptsCount =
          1, // �� �����, ������� ��������� ������������� � �������, ������ ������� �� 8 ���� ������ ���� ��������� �������

        // 2013.07.26 - ���� ������, ��� 0x00FF ������ ������ ����������� ��� ������ � ������
        // 2013.08.15 - ������ ��� ������ � SerialNumber
        Buffer = command.Serialize()
          .GetNetBuffer(
            (ushort) (objectAddress.Way == NetIdRetrieveType.SerialNumber ||
                      objectAddress.Way == NetIdRetrieveType.OldProtocolSerialNumber
              ? 0x00FF
              : objectAddress.Value), command.Code)
      };
      channel.AddCommandToQueueAndExecuteAsync(
        new QueueItem {
          SendingItems = new List<IAddressedSendingItem> {sendingItem},
          OnComplete = results => {
            if (results == null) {
              onComplete(new SendingResultWithAddress(null, new Exception("������ ������� �� ���������� (is null)"),
                null, 0));
            }
            else if (results.Count == 1) {
              var bytes = results[0].Bytes;
              var externalException = results[0].ChannelException;
              if (externalException == null) {
                Log.Log("��� �����: " + bytes.ToText());
                Exception internalException = null;
                byte[] infoBytes = null;
                ushort addressInReply = 0;
                try {
                  // ������������ ��� ��������� � ������� ����� ������
                  bytes.CheckInteleconNetBufCorrect((byte) (sendingItem.Buffer[2] + 10), null);
                  addressInReply = (ushort) (bytes[3] * 0x100 + bytes[4]);
                  Log.Log("����� ���������: " + addressInReply + " ��� 0x" + addressInReply.ToString("X4"));
                  infoBytes = bytes.GetInteleconInfoReplyBytes();
                  Log.Log("����� ��������������� ����: " + infoBytes.ToText());
                }
                catch (Exception ex) {
                  Log.Log(ex.ToString());
                  internalException = ex;
                }
                finally {
                  onComplete(new SendingResultWithAddress(infoBytes, internalException, results[0].Request,
                    addressInReply));
                }
              }
              else onComplete(new SendingResultWithAddress(null, externalException, results[0].Request, 0));
            }
            else
              onComplete(new SendingResultWithAddress(null,
                new Exception("�������� ���������� �������: " + results.Count + " (�������� ���� �����)"), null,
                0)); // ��� ���� �� ����������, ����� ������ ������ ����������, �������, ������� �� ��������
          }
        }, priority);
    }


    public static void SendInteleconCommandToManyProgressive(this IMonoChannel channel, IInteleconCommand command,
      List<ObjectAddress> objects, int timeout, Action<ISendResultWithAddress> onEachComplete) {
      foreach (var objectAddress in objects) {
        channel.SendInteleconCommandAsync(command, objectAddress, timeout, onEachComplete);
      }
    }


    public static void SendManyInteleconCommandsProgressive(this IMonoChannel channel, List<IInteleconCommand> commands,
      ObjectAddress objectAddress, int timeout, Action<ISendResultWithAddress> onEachComplete) {
      foreach (var command in commands) {
        channel.SendInteleconCommandAsync(command, objectAddress, timeout, onEachComplete);
      }
    }
  }
}