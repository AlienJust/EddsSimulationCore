using System.Collections.Generic;

namespace Controllers.Gateway {
	public interface IGatewayControllerInfosSystem {
		IEnumerable<IGatewayControllerInfo> GatewayControllerInfos { get; }
	}
}