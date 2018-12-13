using System.Collections.Generic;

namespace Controllers.Lora {
	struct LoraControllerInfoSimple : ICachedDataControllerConfig {
		public LoraControllerInfoSimple(string name, string deviceId, int dataTtl, int inteleconNetAddress, IReadOnlyList<LoraSubcontrollerInfoSimple> attachedToLoraControllers) {
			Name = name;
			DeviceId = deviceId;
			DataTtl = dataTtl;
			InteleconNetAddress = inteleconNetAddress;
			AttachedToLoraControllers = attachedToLoraControllers;
		}

		public string Name { get; }
		public string DeviceId { get; }
		public int DataTtl { get; }
		public int InteleconNetAddress { get; }
		public IReadOnlyList<LoraSubcontrollerInfoSimple> AttachedToLoraControllers { get; }

		public override string ToString() {
			return "Name: " + Name + ", DeviceId: " + DeviceId + ", DataTtl: " + DataTtl + ", InteleconNetAddress: " + InteleconNetAddress;
		}
	}
}