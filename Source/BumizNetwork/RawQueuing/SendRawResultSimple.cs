using System;
using System.Collections.Generic;
using BumizNetwork.RawQueuing.Contracts;

namespace BumizNetwork.RawQueuing
{
	public sealed class SendRawResultSimple : ISendRawResult
	{
		public IEnumerable<byte> Bytes { get; set; }

		public Exception ChannelException { get; set; }
	}
}
