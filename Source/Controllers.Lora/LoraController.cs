using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using AJ.Std.Loggers;
using AJ.Std.Loggers.Contracts;
using AJ.Std.Text;
using AJ.Std.Text.Contracts;
using Audience;
using Controllers.Contracts;
using nMqtt;
using nMqtt.Messages;

namespace Controllers.Lora
{
    internal sealed class LoraController : IController
    {
        private readonly string _deviceId;
        private readonly MqttClient _client;

        private static readonly ILogger Log = new RelayMultiLogger(true,
            new RelayLogger(Env.GlobalLog,
                new ChainedFormatter(new ITextFormatter[]
                    {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})),
            new RelayLogger(new ColoredConsoleLogger(ConsoleColor.DarkGray, Console.BackgroundColor),
                new ChainedFormatter(new ITextFormatter[]
                    {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})));

        private DateTime? _lastCurrentDataRequestTime;
        private IEnumerable<byte> _lastCurrentDataResult;

        public string Name { get; }


        public LoraController(string name, string deviceId)
        {
            _deviceId = deviceId;
            Name = name;
            //application/1/node/be7a0000000000c8/rx
            var rxTopic2 = "application/1/node/" + _deviceId + "/rx";
            Console.WriteLine(rxTopic2);
            _client = new MqttClient("127.0.0.1", Guid.NewGuid().ToString()); // std port is 1883
            var state = _client.ConnectAsync().Result;
            if (state == ConnectionState.Connected)
            {
                Console.WriteLine("Connected to MQTT broker");
                //client.MessageReceived += OnMessageReceived;
                _client.MessageReceived = OnMessageReceived;
                _client.Subscribe(rxTopic2, Qos.ExactlyOnce);
                //_client.Subscribe(rxTopic2, Qos.ExactlyOnce);
                //Console.WriteLine("Subscribed to " + rxTopic2);
                //for (int i = 0; i < 100; i++)
                //{
                    //_client.Publish(rxTopic2, Encoding.UTF8.GetBytes("123 salem#" + i), Qos.AtLeastOnce);
                    //Thread.Sleep(100);
                //}
                //_client.Publish("rxTopic2", Encoding.UTF8.GetBytes("123 salem#"), Qos.AtLeastOnce);
                
                //var enc = new MqttEncoding();
                //_client.Publish(rxTopic2, enc.GetBytes("123_salem"), Qos.AtLeastOnce);
                //_client.Publish(rxTopic2, Encoding.UTF8.GetBytes("123_salem"), Qos.AtLeastOnce);
                //_client.Publish(rxTopic2, Encoding.UTF8.GetBytes("124_salem"), Qos.AtLeastOnce);
                //_client.Publish(rxTopic2, Encoding.UTF8.GetBytes("125_salem"), Qos.AtLeastOnce);
                
                //_client.Publish(rxTopic2, Encoding.UTF8.GetBytes("125_salem"), Qos.AtLeastOnce);
            }
        }

        private void OnMessageReceived(string topic, byte[] data)
        {
            Console.WriteLine("Received rx " + topic + " >>> " + data.ToText());
            _lastCurrentDataRequestTime = DateTime.Now;
            _lastCurrentDataResult = data;
            // TODO: TO KNOW, WHAT THREAD THIS METHOD CALLED
        }

        public void GetDataInCallback(int command, IEnumerable<byte> data,
            Action<Exception, IEnumerable<byte>> callback)
        {
            if (command == 6)
            {
                var result = data.ToList();
                if (result[3] == 0)
                {
                    Log.Log(Name + " > " + "Запрос текущих данных через шестерку");

                    //NamedLog("Данные взяты из кэша (время жизни кэша до: " + (_lastCurrentDataRequestTime.Value + _cacheTtl).ToString("yyyy.MM.dd-HH:mm:ss") + ") и сейчас будут отправлены: " + _lastCurrentDataResult.ToText());
                    callback(null, _lastCurrentDataResult);
                    //NamedLog("Отправка запроса в менеджер обмена по сети БУМИЗ");
                }
                //else if ((result[3] & 0x06) == 0x06) {
                //NamedLog("Запрос получасовых данных для " + _bumizControllerInfo.Name);
                //var minutes = result[3] == 0x06 ? 0 : 30;
                //var hour = result[4];
                //var day = result[5];
                //var month = result[6];
                //var year = 2000 + result[7];
                //var certainTime = new DateTime(year, month, day, hour, minutes, 0);

                //finally {
                //callback(null, result);
                //}

                else
                {
                    NamedLog("Такая шестерка не поддерживается, будет отправлена пустая посылка");
                    callback(null, result);
                }
            }
            else throw new Exception("Такая команда не поддерживается объектом БУМИЗ");
        }

        private void NamedLog(object obj)
        {
            Log.Log(Name + " > " + obj);
        }
    }
}

namespace Controllers.Lora
{
}