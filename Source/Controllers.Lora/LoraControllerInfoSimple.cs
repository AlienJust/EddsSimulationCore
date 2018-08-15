namespace Controllers.Lora {
  struct LoraControllerInfoSimple {
    public LoraControllerInfoSimple(string name, string deviceId, int dataTtl) {
      Name = name;
      DeviceId = deviceId;
      DataTtl = dataTtl;
    }

    public string Name { get; }
    public string DeviceId { get; }
    public int DataTtl { get; }
  }
}