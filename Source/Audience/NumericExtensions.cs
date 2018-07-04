using System;
using AJ.Std.Loggers;
using AJ.Std.Loggers.Contracts;
using AJ.Std.Text;
using AJ.Std.Text.Contracts;

namespace Audience {
	public static class NumericExtensions
	{
		private static readonly ILogger Log = new RelayMultiLogger(true, new RelayLogger(Env.GlobalLog, new ChainedFormatter(new ITextFormatter[] { new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ") })), new RelayLogger(new ColoredConsoleLogger(ConsoleColor.DarkGray, Console.BackgroundColor), new ChainedFormatter(new ITextFormatter[] { new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ") })));
		public static byte[] ToBytes(this int val)
		{
			var result = new byte[4];
			for (int i = 0; i < 4; ++i)
			{
				var offsetBitsCount = 8 * i;
				result[i] = (byte)((val & (0xFF << offsetBitsCount)) >> offsetBitsCount);
			}
			return result;
		}

		public static byte[] ToBytesInverted(this double val)
		{
			var result = BitConverter.GetBytes(val);
			for (int i = 0; i < 4; ++i)
			{
				byte temp = result[i];
				result[i] = result[7 - i];
				result[7 - i] = temp;
			}
			Log.Log("Converting double: " + val + "  =>  " + result.ToText());
			return result;
		}
		public static byte[] ToBytes(this double val)
		{
			var result = BitConverter.GetBytes(val);
			Log.Log("Converting double: " + val + "  =>  " + result.ToText());
			return result;
		}
		public static byte[] ToBytes(this float val)
		{
			var result = BitConverter.GetBytes(val);
			Log.Log("Converting signle: " + val + "  =>  " + result.ToText());
			return result;
		}
	}
}