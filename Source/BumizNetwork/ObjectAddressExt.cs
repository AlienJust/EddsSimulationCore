using BumizNetwork.Contracts;

namespace BumizNetwork {
	static class ObjectAddressExt {
		public static string GetObjAddrTypeName(this ObjectAddress address) {
			switch (address.Way) {
				case NetIdRetrieveType.InteleconAddress:
					return "ia";
				case NetIdRetrieveType.SerialNumber:
					return "sn";
				case NetIdRetrieveType.OldProtocolSerialNumber:
					return "oldsn";
				default:
					return "unknown";
			}
		}
	}
}