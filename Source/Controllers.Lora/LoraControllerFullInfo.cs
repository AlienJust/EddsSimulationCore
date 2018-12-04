using System.Collections.Generic;
using Controllers.Gateway.Attached;

namespace Controllers.Lora {
	internal class LoraControllerFullInfo {
		public LoraControllerInfoSimple LoraControllerInfo { get; }
		public string RxTopicName { get; }
		public string TxTopicName { get; }
		public AttachedObjectConfig AttachedControllerConfig { get; }

		public IReadOnlyList<LoraSubcontrollerConfig> SubobjectsConfigs { get; }

		public LoraControllerFullInfo(LoraControllerInfoSimple loraControllerInfo, string rxTopicName, string txTopicName, AttachedObjectConfig attachedControllerConfig, IReadOnlyList<LoraSubcontrollerConfig> subobjectsConfigs) {
			LoraControllerInfo = loraControllerInfo;
			RxTopicName = rxTopicName;
			TxTopicName = txTopicName;
			AttachedControllerConfig = attachedControllerConfig;
			SubobjectsConfigs = subobjectsConfigs;
		}

		public override string ToString() {
			return "LoraControllerInfo: {" + LoraControllerInfo + "}, RxTopicName: " + RxTopicName + ", TxTopicName: " + TxTopicName + ", AttachedControllerConfig: {" + AttachedControllerConfig + "}";
		}
	}
}