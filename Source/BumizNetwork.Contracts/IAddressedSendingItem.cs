namespace BumizNetwork.Contracts {
	public interface IAddressedSendingItem {
		ObjectAddress Address { get; }
		byte[] Buffer { get; }
		int AttemptsCount { get; }
		int WaitTimeout { get; }
	}
}