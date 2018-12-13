using System.Dynamic;
using Controllers.Gateway.Attached;

namespace Controllers.Lora {
	internal sealed class LoraSubcontrollerFullInfo {
		public AttachedObjectConfig AttachedConfig { get; }
		public LoraSubcontrollerInfoSimple SubControllerInfo { get; }

		public LoraSubcontrollerFullInfo(LoraSubcontrollerInfoSimple subcontorllerInfo, AttachedObjectConfig attachedConfig) {
			SubControllerInfo = subcontorllerInfo;
			AttachedConfig = attachedConfig;
		}
	}
}
