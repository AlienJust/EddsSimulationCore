using System.Collections.Generic;

namespace Controllers.Gateway.Attached {
	public interface IAttachedControllersInfoSystem
	{
		IEnumerable<IAttachedControllerInfo> AttachedControllerInfos { get; }
	}
}