namespace Controllers.Lora {
	struct LoraControllerInfoSimple {
		public LoraControllerInfoSimple(string name, string deviceId, int dataTtl, int inteleconNetAddress) {
			Name = name;
			DeviceId = deviceId;
			DataTtl = dataTtl;
			InteleconNetAddress = inteleconNetAddress;
		}

		public string Name { get; }
		public string DeviceId { get; }
		public int DataTtl { get; }
		public int InteleconNetAddress { get; }
		public override string ToString() {
			return "Name: " + Name + ", DeviceId: " + DeviceId + ", DataTtl: " + DataTtl + ", InteleconNetAddress: " + InteleconNetAddress;
		}
	}
}