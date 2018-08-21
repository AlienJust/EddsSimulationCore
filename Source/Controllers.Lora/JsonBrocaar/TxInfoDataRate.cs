using Newtonsoft.Json;

namespace Controllers.Lora.JsonBrocaar {
  internal class TxInfoDataRate {
    [JsonProperty(PropertyName = "modulation")]
    public string Modulation { get; set; }

    [JsonProperty(PropertyName = "bandwidth")]
    public int Bandwidth { get; set; }

    [JsonProperty(PropertyName = "spreadFactor")]
    public int SpreadFactor { get; set; }
  }
}