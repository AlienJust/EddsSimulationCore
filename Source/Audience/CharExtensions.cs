namespace Audience {
	public static class CharExtensions {
		public static bool IsBinDigit(this char symbol) {
			return symbol == '0' || symbol == '1';
		}

		public static bool IsOctDigit(this char symbol) {
			return symbol.IsBinDigit()
				|| symbol == '2' || symbol == '3' || symbol == '4' || symbol == '5' || symbol == '6' || symbol == '7';
		}

		public static bool IsDexDigit(this char symbol) {
			return symbol.IsOctDigit() ||
						 symbol == '8' || symbol == '9';
		}

		public static bool IsHexDigit(this char symbol) {
			return symbol.IsDexDigit()
						 || symbol == 'A' || symbol == 'B' || symbol == 'C' || symbol == 'D' || symbol == 'E' || symbol == 'F'
						 || symbol == 'a' || symbol == 'b' || symbol == 'c' || symbol == 'd' || symbol == 'e' || symbol == 'f';
		}
	}
}
