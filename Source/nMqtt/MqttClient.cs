using System;
using System.Data.SqlTypes;
using System.Threading;
using System.Diagnostics;
using nMqtt.Messages;
using System.Threading.Tasks;

namespace nMqtt {
  /// <summary>
  /// Mqtt客户端
  /// </summary>
  public sealed class MqttClient : IDisposable {
    private Timer _pingTimer;
    private MqttConnection _conn;
    private readonly AutoResetEvent _connResetEvent;
    public Action<string, byte[]> MessageReceived;

    /// <summary>
    /// 客户端标识
    /// </summary>
    public string ClientId { get; }

    /// <summary>
    /// 服务器地址
    /// </summary>
    public string Server { get; } = "localhost";

    /// <summary>
    /// 服务器端口
    /// </summary>
    public int Port { get; set; } = 1883;

    public short KeepAlive { get; set; } = 60;

    public bool CleanSession { get; set; } = true;

    public ConnectionState ConnectionState { get; private set; }

    public MqttClient(string server, string clientId = default(string)) {
      Server = server;
      if (string.IsNullOrEmpty(clientId))
        clientId = MqttUtils.NextId();
      ClientId = clientId;
      _conn = new MqttConnection();
      //conn.Recv += DecodeMessage;
      _conn.Recv = DecodeMessage;
      _connResetEvent = new AutoResetEvent(false);
      Console.WriteLine("MqttClient constructor complete");
    }

    /// <summary>
    /// 连接
    /// </summary>
    /// <param name="username">mqtt username</param>
    /// <param name="password">mqtt password</param>
    /// <returns></returns>
    public async Task<ConnectionState> ConnectAsync(string username = default(string),
      string password = default(string)) {
      ConnectionState = ConnectionState.Connecting;
      await _conn.ConnectAsync(Server, Port);

      var msg = new ConnectMessage {
        ClientId = ClientId,
        CleanSession = CleanSession
      };
      if (!string.IsNullOrEmpty(username)) {
        msg.UsernameFlag = true;
        msg.UserName = username;
      }

      if (!string.IsNullOrEmpty(password)) {
        msg.PasswordFlag = true;
        msg.Password = password;
      }

      msg.KeepAlive = KeepAlive;
      _conn.SendMessage(msg);

      if (!_connResetEvent.WaitOne(5000, false)) {
        ConnectionState = ConnectionState.Disconnecting;
        Dispose();
        ConnectionState = ConnectionState.Disconnected;
        return ConnectionState;
      }

      if (ConnectionState == ConnectionState.Connected) {
        _pingTimer = new Timer((state) => { _conn.SendMessage(new PingReqMessage()); }, null, KeepAlive * 1000,
          KeepAlive * 1000);
      }

      return ConnectionState;
    }

    public void Disconnect() {
      //if (conn.Recv != null)
      _conn.Recv = null;
    }

    /// <summary>
    /// 发布消息
    /// </summary>
    /// <param name="topic">主题</param>
    /// <param name="data">数据</param>
    /// <param name="qos">服务质量等级</param>
    public void Publish(string topic, byte[] data, Qos qos = Qos.AtMostOnce) {
      var msg = new PublishMessage {
        FixedHeader = {Qos = qos},
        MessageIdentifier = 0,
        TopicName = topic,
        Payload = data
      };
      Console.WriteLine("Publish message generated, sending...");
      _conn.SendMessage(msg);
    }

    /// <summary>
    /// Subscribes topic
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="qos"></param>
    public void Subscribe(string topic, Qos qos) {
      
      var msg = new SubscribeMessage();
      //msg.FixedHeader.Qos = Qos.AtMostOnce; // if one topic
      //msg.MessageIdentifier = 0;
      
      msg.Subscribe(topic, qos);
      Console.WriteLine("Subscribe message generated (" + topic + "), sending...");
      _conn.SendMessage(msg);
    }

    /// <summary>
    /// 取消订阅
    /// </summary>
    /// <param name="topic"></param>
    public void Unsubscribe(string topic) {
      var msg = new UnsubscribeMessage();
      msg.FixedHeader.Qos = Qos.AtLeastOnce;
      msg.Unsubscribe(topic);
      _conn.SendMessage(msg);
    }

    void DecodeMessage(byte[] buffer) {
      var msg = MqttMessage.DecodeMessage(buffer);
      Console.WriteLine("onRecv:{0}", msg.FixedHeader.MessageType);
      switch (msg.FixedHeader.MessageType) {
        case MessageType.Connack:
          var connAckMsg = (ConnAckMessage) msg;
          Console.WriteLine("onRecv:CONNACK.ConnectReturnCode is {0}", connAckMsg.ConnectReturnCode);
          if (connAckMsg.ConnectReturnCode == ConnectReturnCode.BrokerUnavailable ||
              connAckMsg.ConnectReturnCode == ConnectReturnCode.IdentifierRejected ||
              connAckMsg.ConnectReturnCode == ConnectReturnCode.UnacceptedProtocolVersion ||
              connAckMsg.ConnectReturnCode == ConnectReturnCode.NotAuthorized ||
              connAckMsg.ConnectReturnCode == ConnectReturnCode.BadUsernameOrPassword) {
            ConnectionState = ConnectionState.Disconnecting;
            Dispose();
            _conn = null;
            ConnectionState = ConnectionState.Disconnected;
          }
          else {
            ConnectionState = ConnectionState.Connected;
            Console.WriteLine("DecodeMessage > ConnectionState.Connected!");
          }

          _connResetEvent.Set();
          break;
        case MessageType.Publish:
          Console.WriteLine("DecodeMessage > MessageType.PUBLISH");
          var pubMsg = (PublishMessage) msg;
          string topic = pubMsg.TopicName;
          var data = pubMsg.Payload;
          if (pubMsg.FixedHeader.Qos == Qos.AtLeastOnce) {
            var ackMsg = new PublishAckMessage {
              MessageIdentifier = pubMsg.MessageIdentifier
            };
            _conn.SendMessage(ackMsg);
          }

          OnMessageReceived(topic, data);
          break;
        case MessageType.Puback:
          var pubAckMsg = (PublishAckMessage) msg;
          Console.WriteLine("PUBACK MessageIdentifier:" + pubAckMsg.MessageIdentifier);
          break;
        case MessageType.Pubrec:

          break;
        case MessageType.Pubrel:
          break;
        case MessageType.Pubcomp:
          break;
        case MessageType.Subscribe:
          break;
        case MessageType.Suback:
          var subAckMsg = (SubscribeAckMessage) msg;
          Console.WriteLine("PUBACK MessageIdentifier:" + subAckMsg.MessageIdentifier);
          break;
        case MessageType.Unsubscribe:
          break;
        case MessageType.Unsuback:
          break;
        case MessageType.Pingreq:
          _conn.SendMessage(new PingRespMessage());
          break;
        case MessageType.Disconnect:
          Disconnect();
          break;
      }
    }

    void OnMessageReceived(string topic, byte[] data) {
      MessageReceived?.Invoke(topic, data);
    }

    void Close() {
      if (ConnectionState == ConnectionState.Connecting) {
        // TODO: Decide what to do if the caller tries to close a connection that is in the process of being connected.
      }

      if (ConnectionState == ConnectionState.Connected) {
        Disconnect();
      }
    }

    public void Dispose() {
      if (_conn != null) {
        Close();
        if (_conn != null) {
          //connection.Dispose();
        }
      }

      _pingTimer?.Dispose();
      GC.SuppressFinalize(this);
    }
  }
}