using Commands.Contracts;

namespace Commands.Bumiz.Intelecon {
	public class WriteInteleconAddressCommand : IInteleconCommand {
		public uint PinCode { get; }
		public ushort InteleconAddress { get; }

		public WriteInteleconAddressCommand(uint pinCode, ushort inteleconAddres) {
			PinCode = pinCode;
			InteleconAddress = inteleconAddres;
		}

		public byte Code => 0x09;

		public string Comment => "Запись адреса интелекон";

		public byte[] Serialize() {
			var query = new byte[7];
			query[0] = 0x01;
			query[1] = (byte) (PinCode & 0x000000FF);
			query[2] = (byte) ((PinCode & 0x0000FF00) >> 8);
			query[3] = (byte) ((PinCode & 0x00FF0000) >> 16);
			query[4] = (byte) ((PinCode & 0xFF000000) >> 24);

			query[5] = (byte) (InteleconAddress & 0x00FF);
			query[6] = (byte) ((InteleconAddress & 0xFF00) >> 8);

			return query;
		}

		public bool GetResult(byte[] reply) {
			return reply.Length == 1 && reply[0] == 0x01;
		}
	}
}
