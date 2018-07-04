using System;
using System.Collections.Generic;
using BumizNetwork.Contracts;

namespace BumizNetwork.Shared {
	/// <summary>
	/// Элемент очереди обмена
	/// </summary>
	public sealed class QueueItem : IQueueAddressItem {
		public Action<List<ISendResult>> OnComplete { get; set; }

		public List<IAddressedSendingItem> SendingItems { get; set; }
	}
}