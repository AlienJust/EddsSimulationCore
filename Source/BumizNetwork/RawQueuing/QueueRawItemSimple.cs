using System;
using BumizNetwork.RawQueuing.Contracts;

namespace BumizNetwork.RawQueuing
{
	public sealed class QueueRawItemSimple : IQueueRawItem
	{
		public ISendRawItem SendItem { get; set; }
		public Action<ISendRawResult> OnSendComplete { get; set; }
	}
}
