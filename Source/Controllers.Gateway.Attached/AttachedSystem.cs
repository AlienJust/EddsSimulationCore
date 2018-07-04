using System.Collections.Generic;
using System.Composition;
using System.IO;
using AJ.Std.Composition;
using AJ.Std.Composition.Contracts;
using Audience;
using GatewayAttachedControllers;

namespace Controllers.Gateway.Attached
{
	[Export(typeof(ICompositionPart))]
	class AttachedControllersInfoSystem : CompositionPartBase, IAttachedControllersInfoSystem {
		public AttachedControllersInfoSystem() {
			AttachedControllerInfos = XmlFactory.GetCounterCorrectionInfosFromXml(Path.Combine(Env.CfgPath, "AttachedControllerInfos.xml"));
		}

		public IEnumerable<IAttachedControllerInfo> AttachedControllerInfos { get; }


		public override void SetCompositionRoot(ICompositionRoot root) {
			// Get all needed c.parts with adding refs to them
		}

		public override string Name => "GatewayAttachedControllers";
		public override void BecameUnused()
		{
			// Unload all c.parts here
		}
	}
}
