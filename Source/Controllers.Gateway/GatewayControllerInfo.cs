namespace Controllers.Gateway {
	class GatewayControllerInfo : IGatewayControllerInfo {
		public string Name { get; }

		public GatewayControllerInfo(string name) {
			Name = name;
		}
	}
}