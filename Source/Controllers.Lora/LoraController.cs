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
    private readonly string _devEui;
    private readonly string _mqttTopicPrefix;
    private readonly Action<string> _logAction;
    private readonly MqttClient _client;

    private DateTime _lastCurrentDataRequestTime;
    private readonly TimeSpan _cacheInvalidationTime;
    
    private IReadOnlyList<byte> _lastCurrentDataResult;

    public string Name { get; }


    public LoraController(string name, string devEui, string mqttTopicPrefix, Action<string> logAction, MqttClient client) {
      Name = name;
      _devEui = devEui; // string like "be7a0000000000c8", it is unique for each LORA controller
      _mqttTopicPrefix = mqttTopicPrefix;
      _logAction = logAction;

      _lastCurrentDataRequestTime = DateTime.MinValue;
      _lastCurrentDataResult = new byte[0];
      _cacheInvalidationTime = TimeSpan.FromMinutes(5);

      var rxTopic = mqttTopicPrefix + _devEui + "/rx";
      
      _client = client;
      _client.MessageReceived += OnMessageReceived;
      _client.Subscribe(rxTopic, Qos.ExactlyOnce);
      NamedLog("Subscribed to RX topic " + rxTopic);

      // TODO: PUBLISH string like: 
      //_client.Publish(rxTopic2, Encoding.UTF8.GetBytes("123_salem"), Qos.AtLeastOnce);
      //_client.Publish(rxTopic2, Encoding.UTF8.GetBytes("124_salem"), Qos.AtLeastOnce);
      //_client.Publish(rxTopic2, Encoding.UTF8.GetBytes("125_salem"), Qos.AtLeastOnce);

      //_client.Publish(rxTopic2, Encoding.UTF8.GetBytes("125_salem"), Qos.AtLeastOnce);
    }

    private void OnMessageReceived(object sender, MessageReceivedEventArgs args) {
      NamedLog("Received rx " + args.Topic + " >>> " + args.Data.ToText());
      try {
        // TODO: TO KNOW, WHAT THREAD THIS METHOD CALLED
        // Need to decode string like: //{"applicationID":"1","applicationName":"mgf_vega_nucleo_debug_app","deviceName":"mgf","devEUI":"be7a0000000000c8","deviceStatusBattery":254,"deviceStatusMargin":26,"rxInfo":[{"mac":"0000e8eb11417531","time":"2018-07-05T10:20:46.12777Z","rssi":-46,"loRaSNR":7.2,"name":"vega-gate","latitude":55.95764,"longitude":60.57098,"altitude":317}],"txInfo":{"frequency":868500000,"dataRate":{"modulation":"LORA","bandwidth":125,"spreadFactor":7},"adr":true,"codeRate":"4/5"},"fCnt":2502,"fPort":2,"data":"/////w=="}
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
    }

    public void GetDataInCallback(int command, IEnumerable<byte> data,
      Action<Exception, IEnumerable<byte>> callback) {
      if (command == 6) {
        var result = data.ToList();
        if (result[3] == 0) {
          NamedLog("SCADA requested accepted, command code is 6, data type - current");

          
          if (DateTime.Now - _lastCurrentDataRequestTime < _cacheInvalidationTime) {
            NamedLog("Data was taken from cache, send it back to SCADA");
            result.AddRange(_lastCurrentDataResult);
          }
          else NamedLog("Cache data are too old, will NOT send it!");

          callback(null, result);


          //NamedLog("Отправка запроса в менеджер обмена по сети БУМИЗ");
        }
        else if (result[3] == 0x06 || result[3] == 0x07) {
          NamedLog("SCADA requested accepted, command code is 6, data type - half an hour, but nothing to send");
          callback(null, result);
        }
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
          NamedLog("SCADA requested accepted, command code is 6, data type = " + result[3]);
          callback(null, result);
        }
      }
      else throw new Exception("Such command is not supported by LORA controller");
    }

    private void NamedLog(object obj) {
      _logAction(Name + " > " + obj);
    }
  }
}