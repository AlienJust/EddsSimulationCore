using System;

namespace Controllers.Lora {
	interface IAttachedLastDataCache {
		void AddData(string controllerId, int config, byte[] data);

		Tuple<DateTime, byte[]> GetData(string controllerId, int config);
	}
}