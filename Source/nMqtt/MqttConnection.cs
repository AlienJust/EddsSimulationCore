using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using nMqtt.Messages;
using System.Threading.Tasks;
using AJ.Std.Text;

namespace nMqtt
{
    internal sealed class MqttConnection
    {
        private Socket _socket;

        /// <summary>
        /// Connections count?
        /// </summary>
        private readonly int _mNConnection = 1;

        public Action<byte[]> Recv; // will be raised, when mqtt data received

        /// <summary>
        /// Socket async event args pool 
        /// </summary>
        private readonly SocketAsyncEventArgsPool _socketAsynPool;

        public MqttConnection()
        {
            const int receiveBufferSize = 4096;
            var bufferManager = new BufferManager(receiveBufferSize * _mNConnection, receiveBufferSize);
            bufferManager.ResetBuffer();
            _socketAsynPool = new SocketAsyncEventArgsPool(_mNConnection);

            //按照连接数建立读写对象
            for (int i = 0; i < _mNConnection; i++)
            {
                var args = new SocketAsyncEventArgs();
                args.Completed += IoCompleted;
                args.UserToken = new RecvToken();
                bufferManager.SetBuffer(args);
                _socketAsynPool.Push(args);
            }
        }

        /// <summary>
        /// Connects to broker
        /// </summary>
        /// <param name="server">Server address or hostname</param>
        /// <param name="port">TCP port</param>
        public async Task ConnectAsync(string server, int port)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.DontFragment = true;
            //_socket.DualMode = false;

            Console.WriteLine("Connecting");
            await _socket.ConnectAsync(server, port);
            Console.WriteLine("Connected, starting receive...");
            _socket.ReceiveAsync(_socketAsynPool.Pop());
        }

        void ProcessRecv(SocketAsyncEventArgs e)
        {
            Console.WriteLine("[MqttConnection] ----------------------- ProcessRecv:{0}", e.BytesTransferred);
            if (e.UserToken is RecvToken token)
            {
                if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
                {
                    var buffer = new byte[e.BytesTransferred];
                    Buffer.BlockCopy(e.Buffer, e.Offset, buffer, 0, buffer.Length);

                    token.Buffer.AddRange(buffer);
                    Console.WriteLine("[MqttConnection] RecvToken.Buffer.Length = : " + token.Buffer.Count);
                    if (token.IsReadComplete)
                    {
                        Console.WriteLine("RecvToken.Buffer read is complete");
                        Recv?.Invoke(token.Buffer.ToArray());
                        token.Reset();
                    }
                    else Console.WriteLine("RecvToken.Buffer read is NOT complete");
                }
                else
                {
                    Console.WriteLine("token.Reset()");
                    token.Reset();
                    //socketAsynPool.Push(e);
                    //socket.ReceiveAsync(e);
                }

                Console.WriteLine("Continue receiving...");
                _socket.ReceiveAsync(e); // Continue receiving...
            }
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(MqttMessage message)
        {
            Console.WriteLine("onSend:{0}", message.FixedHeader.MessageType);
            using (var stream = new MemoryStream())
            {
                message.Encode(stream);
                stream.Seek(0, SeekOrigin.Begin);
                var dataArray = stream.ToArray();
                //Console.WriteLine("Will be sended to socket: " + dataArray.ToText());
                //Console.WriteLine("Will be sended to socket: " + Encoding.UTF8.GetString(dataArray));
                _socket.Send(stream.ToArray(), SocketFlags.None);
                Console.WriteLine("[MqttConnection] Sended: " + dataArray.Length + " bytes");
            }
        }

        private void IoCompleted(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessRecv(e);
                    break;
                default:
                    throw new ArgumentException("nError in I/O Completed");
            }
        }
    }
}