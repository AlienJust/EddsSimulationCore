using System;

namespace BumizNetwork.RawQueuing.Contracts {
	public interface IQueueRawItem {
		ISendRawItem SendItem { get; }
		Action<ISendRawResult> OnSendComplete { get; }
	}
}