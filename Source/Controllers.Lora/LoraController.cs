using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AJ.Std.Text;
using Controllers.Contracts;
using nMqtt;
using nMqtt.Messages;

namespace Controllers.Lora {
  internal sealed class LoraController : IController {
    private readonly string _deviceId;
    private readonly string _mqttTopicPrefix;
    private readonly Action<string> _logAction;
    private readonly MqttClient _client;

    private DateTime _lastCurrentDataRequestTime;
    private readonly TimeSpan _cacheInvalidationTime;
    
    private IReadOnlyList<byte> _lastCurrentDataResult;

    public string Name { get; }


    public LoraController(string name, string deviceId, string mqttTopicPrefix, Action<string> logAction) {
      _deviceId = deviceId;
      _mqttTopicPrefix = mqttTopicPrefix;
      _logAction = logAction;
      Name = name;

      _lastCurrentDataRequestTime = DateTime.MinValue;
      _lastCurrentDataResult = new byte[0];
      _cacheInvalidationTime = TimeSpan.FromMinutes(5);

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
      try {
        // TODO: TO KNOW, WHAT THREAD THIS METHOD CALLED
        var rawJson = Encoding.UTF8.GetString(args.Data);
        NamedLog("Parsed RX >>> " + rawJson);
        var lastData = rawJson.Split(",\"data\":").Last();
        lastData = lastData.Substring(1, lastData.Length - 3);
        NamedLog("Parsed RX LAST : >>> " + lastData);
        var decodedBytes = Convert.FromBase64String(lastData);
        NamedLog("Decoded bytes are: " + decodedBytes.ToText());

        _lastCurrentDataResult = decodedBytes; // copy data
        _lastCurrentDataRequestTime = DateTime.Now; // remember time
        //NamedLog("Float RX DATA >>>" + BitConverter.ToSingle(decodedBytes));
      }
      catch (Exception e) {
        Console.WriteLine(e);
        throw;
      }

      //NamedLog("JSON data vaule: " + data.data);
      //{"applicationID":"1","applicationName":"mgf_vega_nucleo_debug_app","deviceName":"mgf","devEUI":"be7a0000000000c8","deviceStatusBattery":254,"deviceStatusMargin":26,"rxInfo":[{"mac":"0000e8eb11417531","time":"2018-07-05T10:20:46.12777Z","rssi":-46,"loRaSNR":7.2,"name":"vega-gate","latitude":55.95764,"longitude":60.57098,"altitude":317}],"txInfo":{"frequency":868500000,"dataRate":{"modulation":"LORA","bandwidth":125,"spreadFactor":7},"adr":true,"codeRate":"4/5"},"fCnt":2502,"fPort":2,"data":"/////w=="}
    }

    public void GetDataInCallback(int command, IEnumerable<byte> data,
      Action<Exception, IEnumerable<byte>> callback) {
      if (command == 6) {
        var result = data.ToList();
        if (result[3] == 0) {
          NamedLog("Поступил запрос текущих данных через шестерку");

          
          if (DateTime.Now - _lastCurrentDataRequestTime < _cacheInvalidationTime) {
            NamedLog("Данные взяты из кэша"); // TODO: cache lifetime
            result.AddRange(_lastCurrentDataResult);
          }
          else NamedLog("Cache data are too old, will NOT send it!");

          callback(null, result);


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