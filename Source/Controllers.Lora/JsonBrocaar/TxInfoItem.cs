using Newtonsoft.Json;

namespace Controllers.Lora.JsonBrocaar {
  internal class TxInfoItem {
    [JsonProperty(PropertyName = "frequency")]
    public int Frequency { get; set; }

    [JsonProperty(PropertyName = "dataRate")]
    public TxInfoDataRate DataRate { get; }

    [JsonProperty(PropertyName = "adr")] public bool Adr { get; set; }

    [JsonProperty(PropertyName = "codeRate")]
    public string CodeRate { get; set; }
  }
}