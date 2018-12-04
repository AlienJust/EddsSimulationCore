using Controllers.Gateway.Attached;

namespace Controllers.Lora {
	internal sealed class LoraSubcontrollerConfig {
		public AttachedObjectConfig AttachedConfig { get; }
		public string Name { get; }

		public LoraSubcontrollerConfig(string name, AttachedObjectConfig attachedConfig) {
			Name = name;
			AttachedConfig = attachedConfig;
		}
	}
}
