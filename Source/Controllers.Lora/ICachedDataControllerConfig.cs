namespace Controllers.Lora {
	internal interface ICachedDataControllerConfig {
		string Name { get; }
		int DataTtl { get; }
	}
}