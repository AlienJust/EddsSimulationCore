namespace Controllers.Gateway.Attached {
	class AttachedControllerInfo : IAttachedControllerInfo {
		public AttachedControllerInfo(string gateway, int channel, int type, int number, string name) {
			Gateway = gateway;
			Channel = channel;
			Type = type;
			Number = number;
			Name = name;
		}

		public string Name { get; }

		public string Gateway { get; }

		public int Channel { get; }

		public int Type { get; }

		public int Number { get; }
	}
}