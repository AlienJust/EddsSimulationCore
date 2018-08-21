using Newtonsoft.Json;

namespace Controllers.Lora.JsonBrocaar {
  internal class RxInfoItem {
    [JsonProperty(PropertyName = "mac")] public string Mac { get; set; }

    [JsonProperty(PropertyName = "time")] public string Time { get; set; }

    [JsonProperty(PropertyName = "rssi")] public int Rssi { get; set; }

    [JsonProperty(PropertyName = "loRaSNR")]
    public double LoraSnr { get; set; }

    [JsonProperty(PropertyName = "name")] public string Name { get; set; }

    [JsonProperty(PropertyName = "latitude")]
    public double Latitude { get; set; }

    [JsonProperty(PropertyName = "longitude")]
    public double Longitude { get; set; }

    [JsonProperty(PropertyName = "altitude")]
    public int Altitude { get; set; }
  }
}