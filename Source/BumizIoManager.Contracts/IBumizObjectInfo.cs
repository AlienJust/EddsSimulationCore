using BumizNetwork.Contracts;

namespace BumizIoManager.Contracts {
	public interface IBumizObjectInfo {
		string Name { get; }
		string ChannelName { get; }
		ObjectAddress Address { get; }
		int Timeout { get; }
	}

	public sealed class BumizObjectInfo : IBumizObjectInfo {
		public string Name { get; }
		public string ChannelName { get; }
		public ObjectAddress Address { get; }
		public int Timeout { get; }
		public BumizObjectInfo(string name,string channelName, ObjectAddress address, int timeout) {
			Name = name;
			ChannelName = channelName;
			Address = address;
			Timeout = timeout;
		}
	}
}