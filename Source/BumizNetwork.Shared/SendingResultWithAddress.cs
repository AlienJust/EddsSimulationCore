using System;
using BumizNetwork.Contracts;

namespace BumizNetwork.Shared {
	public class SendingResultWithAddress : ISendResultWithAddress {
		public IAddressedSendingItem Request { get; }
		public byte[] Bytes { get; }
		public Exception ChannelException { get; }
		public ushort AddressInReply { get; }

		public SendingResultWithAddress(byte[] bytes, Exception channelException, IAddressedSendingItem request, ushort addressInReply) {
			Bytes = bytes;
			ChannelException = channelException;
			Request = request;
			AddressInReply = addressInReply;
		}
	}
}