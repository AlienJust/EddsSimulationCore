using System;
using System.Collections.Generic;

namespace BumizNetwork.RawQueuing.Contracts {
	public interface ISendRawResult {
		IEnumerable<byte> Bytes { get; }
		Exception ChannelException { get; }

		// Если Bytes = 0 и ChannelException == null, значит 
	}
}