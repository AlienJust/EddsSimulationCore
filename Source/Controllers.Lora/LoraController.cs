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

namespace Controllers.Lora {
  internal sealed class LoraController : IController {
    private readonly string _deviceId;
    private readonly string _mqttTopicPrefix;
    private readonly Action<string> _logAction;
    private readonly MqttClient _client;

    private DateTime? _lastCurrentDataRequestTime;
    private IEnumerable<byte> _lastCurrentDataResult;

    public string Name { get; }


    public LoraController(string name, string deviceId, string mqttTopicPrefix, Action<string> logAction) {
      _deviceId = deviceId;
      _mqttTopicPrefix = mqttTopicPrefix;
      _logAction = logAction;
      Name = name;

      var rxTopic = mqttTopicPrefix + _deviceId + "/rx";
      Console.WriteLine(rxTopic);
      _client = new MqttClient("127.0.0.1", Guid.NewGuid().ToString()); // std port is 1883
      var state = _client.ConnectAsync().Result;
      if (state == ConnectionState.Connected) {
        _logAction("Connected to MQTT broker");
        _client.MessageReceived += OnMessageReceived;
        _client.Subscribe(rxTopic, Qos.ExactlyOnce);

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

    private void OnMessageReceived(object sender, MessageReceivedEventArgs args) {
      NamedLog("Received rx " + args.Topic + " >>> " + args.Data.ToText());
      _lastCurrentDataRequestTime = DateTime.Now; // remember time
      _lastCurrentDataResult = args.Data.ToArray(); // copy data
      // TODO: TO KNOW, WHAT THREAD THIS METHOD CALLED
    }

    public void GetDataInCallback(int command, IEnumerable<byte> data,
      Action<Exception, IEnumerable<byte>> callback) {
      if (command == 6) {
        var result = data.ToList();
        if (result[3] == 0) {
          NamedLog("Поступил запрос текущих данных через шестерку");

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

        else {
          NamedLog("Такая шестерка не поддерживается, будет отправлена пустая посылка");
          callback(null, result);
        }
      }
      else throw new Exception("Такая команда не поддерживается объектом БУМИЗ");
    }

    private void NamedLog(object obj) {
      _logAction(Name + " > " + obj);
    }
  }
}