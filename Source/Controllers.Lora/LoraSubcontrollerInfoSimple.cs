namespace Controllers.Lora {
	struct LoraSubcontrollerInfoSimple : ICachedDataControllerConfig {
		public LoraSubcontrollerInfoSimple(string name, int dataTtl) {
			Name = name;
			DataTtl = dataTtl;
		}
		public string Name { get; }
		public int DataTtl { get; }

	}
}