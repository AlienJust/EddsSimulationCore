using System;
using System.Collections.Generic;

namespace BumizNetwork.Contracts {
	public interface IQueueAddressItem {
		/// <summary>
		/// Будет вызвано в другом потоке!
		/// </summary>
		Action<List<ISendResult>> OnComplete { get; }

		List<IAddressedSendingItem> SendingItems { get; }
	}
}