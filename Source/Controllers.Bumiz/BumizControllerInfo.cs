namespace Controllers.Bumiz {
	internal class BumizControllerInfo : IBumizControllerInfo {
		public BumizControllerInfo(string name, int currentDataCacheTtlSeconds, string pulse1Expression, string pulse2Expression, string pulse3Expression) {
			Name = name;
			CurrentDataCacheTtlSeconds = currentDataCacheTtlSeconds;
			Pulse1Expression = pulse1Expression;
			Pulse2Expression = pulse2Expression;
			Pulse3Expression = pulse3Expression;
		}

		public string Name { get; }

		public int CurrentDataCacheTtlSeconds { get; }

		public string Pulse1Expression { get; }

		public string Pulse2Expression { get; }

		public string Pulse3Expression { get; }
	}
}