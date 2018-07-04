using System.Collections.Generic;

namespace PollServiceProxy {
	public interface IScadaObjectInfo {
		string Name { get; }
		bool SendMicroPackets { get; }
		IEnumerable<IScadaAddress> ScadaAddresses { get; }
	}
}