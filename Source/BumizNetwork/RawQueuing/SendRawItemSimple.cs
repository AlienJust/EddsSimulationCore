using System.Collections.Generic;
using System.Text;
using BumizNetwork.RawQueuing.Contracts;

namespace BumizNetwork.RawQueuing
{
	public sealed class SendRawItemSimple : ISendRawItem
	{
		private SendRawItemSimple() {
			// it's private so nobody can init object directly
		}

		public IEnumerable<byte> SendingBytes { get; private set; }
		public int? AwaitedBytesCount { get; private set; } 

		public static SendRawItemSimple FromAscii(string asciiString, int? awaitedBytesCount) {
			return new SendRawItemSimple{AwaitedBytesCount = awaitedBytesCount, SendingBytes = Encoding.ASCII.GetBytes(asciiString)};
		}

		public static SendRawItemSimple FromBytes(IEnumerable<byte> bytes, int? awaitedBytesCount)
		{
			return new SendRawItemSimple { AwaitedBytesCount = awaitedBytesCount, SendingBytes = bytes };
		}
	}
}
