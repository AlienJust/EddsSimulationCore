using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Audience {
	public static class Extensions {
		public static byte CtrlSum(this IEnumerable<byte> info) {
			byte ctrlSum = info.Aggregate<byte, byte>(0, (current, b) => (byte) (current + b));
			ctrlSum = (byte) (0xFF - ctrlSum + 1);
			return ctrlSum;
		}


		public static byte DallasCrc8(this IEnumerable<byte> info) {
			byte crc = 0; // величина циклической суммы

			foreach (var b in info) //по всем байтам
			{
				var byteIn = b; //Вдвигамемый в сумму байт
				for (int j = 0; j < 8; j++) //по всем битам этого байта
				{
					var bitIn = ((byteIn ^ crc) & 0x01) == 0x01; //вдвигаемый в сумму бит
					if (bitIn) crc ^= 0x18; //маска циклической суммы
					crc >>= 1; //сдвинуть сумму
					if (bitIn) crc |= 0x80; //вдвинуть бит
					byteIn >>= 1; //сдвинуть текущий байт
				}
			}

			return crc;
		}


		public static byte[] GetNetBuffer(this byte[] buffer, ushort netAddress, byte commandCode) {
			var netAddrB1 = (byte) ((netAddress & 0xFF00) >> 8);
			var netAddrB0 = (byte) (netAddress & 0x00FF);
			//                                  00    01                         02           03         04
			var resultBuffer = new List<byte> {
				0x7A,
				(byte) (buffer.Length + 4),
				commandCode,
				netAddrB1,
				netAddrB0
			};
			resultBuffer.AddRange(buffer);
			var dallasCrc = DallasCrc8(resultBuffer.GetRange(1, resultBuffer.Count - 1));
			resultBuffer.Add(dallasCrc);
			var ctrlSum = CtrlSum(resultBuffer.GetRange(1, resultBuffer.Count - 1));
			resultBuffer.Add(ctrlSum);
			resultBuffer.Add(0x0D); // Stop Byte

			return resultBuffer.ToArray();
		}

		/*public static string ToText(this IEnumerable<byte> data) {
		  int count = 0;
		  string result = string.Empty;
		  foreach (byte b in data) {
		    result += b.ToString("X2") + " ";
		    count++;
		  }
		  result += "[" + count + "]";
		  return result;
		}*/

		/*public static string ToText(this IEnumerable<byte> data, bool showLength) {
		  int count = 0;
		  string result = string.Empty;
		  foreach (byte b in data) {
		    result += b.ToString("X2") + " ";
		    count++;
		  }
		  if (showLength) result += "[" + count + "]";
		  else result = result.Substring(0, result.Length - 1); // to remove last space
		  return result;
		}*/


		/// <summary>
		/// Получает буфер для установки DNetID
		/// </summary>
		/// <param name="dNetId">Динамический ИД</param>
		/// <param name="queryLength">Длина последующего запроса</param>
		/// <returns>Результирующий буфер</returns>
		public static byte[] GetBufferSetDNetId(this ushort dNetId, ushort queryLength) {
			//                    0123456789ABCDEF0  1
			const string query = "AT+UCASTB:00,0000\x0D";
			var bytes = Encoding.ASCII.GetBytes(query);

			var lenBytes = Encoding.ASCII.GetBytes(queryLength.ToString("X2"));
			bytes[10] = lenBytes[0];
			bytes[11] = lenBytes[1];

			bytes[16] = ((byte) ((dNetId & 0x000F))).VtoX();
			bytes[15] = ((byte) ((dNetId & 0x00F0) >> 04)).VtoX();
			bytes[14] = ((byte) ((dNetId & 0x0F00) >> 08)).VtoX();
			bytes[13] = ((byte) ((dNetId & 0xF000) >> 12)).VtoX();

			return bytes;
		}

		public static byte VtoX(this byte b) {
			return b > 9 ? (byte) (b + 48 + 7) : (byte) (b + 48);
		}

		public static byte XtoV(this byte b) {
			return b > 60 ? (byte) (b - 55) : (byte) (b - 48);
		}


		/// <summary>
		/// Specifies the current day of the week; Sunday = 0, Monday = 1, and so on. 
		/// </summary>
		/// <param name="dt"></param>
		/// <returns>day number of the week</returns>
		public static int GetDayOfWeekNumber(this DateTime dt) {
			switch (dt.DayOfWeek) {
				case DayOfWeek.Monday:
					return 1;
				case DayOfWeek.Tuesday:
					return 2;
				case DayOfWeek.Wednesday:
					return 3;
				case DayOfWeek.Thursday:
					return 4;
				case DayOfWeek.Friday:
					return 5;
				case DayOfWeek.Saturday:
					return 6;
				case DayOfWeek.Sunday:
					return 0;
			}

			throw new Exception("Cannot get day of week number");
		}


		public static byte[] GetInteleconInfoReplyBytes(this byte[] inteleconReply) {
			var result = new byte[inteleconReply.Length - 8];
			for (int i = 0; i < result.Length; ++i) {
				result[i] = inteleconReply[i + 5];
			}

			return result;
		}


		public static void CheckInteleconNetBufCorrect(this byte[] netBuf, byte? cmdCode, ushort? inteleconAddr) {
			if (netBuf.Length < 8) throw new Exception("Reply is too short");
			if (netBuf[0] != 0x7A) throw new Exception("Wrong start byte");
			if (netBuf[netBuf.Length - 1] != 0x0D) throw new Exception("Wrong stop byte");
			if (netBuf.Length != netBuf[1] + 4) throw new Exception("Wrong length");
			//if (netBuf.Length != awaitedBufLength) throw new Exception("Wrong info size (" + netBuf.Length + "), expected " + awaitedBufLength);

			if (cmdCode != null)
				if (netBuf[2] != cmdCode)
					throw new Exception("Wrong command code");

			if (inteleconAddr != null)
				if (netBuf[3] * 0x100 + netBuf[4] != inteleconAddr)
					throw new Exception("Wrong net address");


			if (netBuf[netBuf.Length - 1 - 2] != netBuf.ToList().GetRange(1, netBuf.Length - 1 - 3).DallasCrc8())
				throw new Exception("Wrong CRC");
			if (netBuf[netBuf.Length - 1 - 1] != netBuf.ToList().GetRange(1, netBuf.Length - 1 - 2).CtrlSum())
				throw new Exception("Wrong CS");
		}


		public static byte[] GetInteleconInfoFromNetBuf(this byte[] netBuf) {
			return netBuf.ToList().GetRange(5, netBuf.Length - 8).ToArray();
		}


		public static byte BinaryToBcd(this byte b) {
			return byte.Parse(b.ToString("D2"), NumberStyles.HexNumber);
		}


		public static byte BcdToBinary(this byte b) {
			return byte.Parse(b.ToString("X2"));
		}
	}
}