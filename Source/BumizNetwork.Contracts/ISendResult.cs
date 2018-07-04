using System;

namespace BumizNetwork.Contracts {
	public interface ISendResult {
		IAddressedSendingItem Request { get; }
		byte[] Bytes { get; }
		Exception ChannelException { get; }
	}
}