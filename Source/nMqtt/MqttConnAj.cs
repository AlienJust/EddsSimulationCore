using System;
using System.Net.Sockets;
using System.Threading;
using AJ.Std.Concurrent;
using AJ.Std.Concurrent.Contracts;

namespace nMqtt {
  public class MqttConnAj {
    private readonly TcpClient _client;
    private readonly IWorker<Action> _receiveWorker;
    private readonly IWorker<Action> _sendingWorker;

    public MqttConnAj(string host, int port, string username, string password) {
      _client = new TcpClient(host, port);
      _receiveWorker =
        new SingleThreadedRelayQueueWorkerProceedAllItemsBeforeStopNoLog<Action>("ReceiveMqtt", a => a(),
          ThreadPriority.BelowNormal, true, null);
      _sendingWorker =
        new SingleThreadedRelayQueueWorkerProceedAllItemsBeforeStopNoLog<Action>("SendingMqtt", a => a(),
          ThreadPriority.BelowNormal, true, null);

      _receiveWorker.AddWork(() => {
        while (true) // TODO: stop receiving
        {
          //_client.GetStream().Read(buffer, 0, count)
        }
      });
    }
  }
}