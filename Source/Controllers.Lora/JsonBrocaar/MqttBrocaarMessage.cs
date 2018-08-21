using Newtonsoft.Json;

namespace Controllers.Lora.JsonBrocaar {
  internal class MqttBrocaarMessage {
    [JsonProperty(PropertyName = "applicationId")]
    public int ApplicationId { get; set; }

    [JsonProperty(PropertyName = "applicationName")]
    public string ApplicationName { get; set; }

    [JsonProperty(PropertyName = "deviceName")]
    public string DeviceName { get; set; }

    [JsonProperty(PropertyName = "devEUI")]
    public string DevEui { get; set; }

    [JsonProperty(PropertyName = "deviceStatusBattery")]
    public int DeviceStatusBattery { get; set; }

    [JsonProperty(PropertyName = "deviceStatusMargin")]
    public int DeviceStatusMargin { get; set; }

    [JsonProperty(PropertyName = "rxInfo")]
    public RxInfoItem[] RxInfo { get; set; }

    [JsonProperty(PropertyName = "txInfo")]
    public TxInfoItem TxInfo { get; set; }

    [JsonProperty(PropertyName = "fCnt")] public int Fcnt { get; set; }

    [JsonProperty(PropertyName = "fPort")] public int Fport { get; set; }

    [JsonProperty(PropertyName = "data")] public string Data { get; set; }
  }
}