using System;
using System.Collections.Generic;

namespace PollServiceProxy.Contracts {
	/// <summary>
	/// Подсистема объектов (например БУМИЗ)
	/// </summary>
	public interface ISubSystem {
		string SystemName { get; }
		void ReceiveData(string uplinkName, string subObjectName, byte commandCode, byte[] data, Action notifyOperationComplete, Action<int, IEnumerable<byte>> sendReplyAction);
		//void SetGateway(IPollGateway gateway);
	}
}