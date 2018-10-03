using System;
using System.Collections.Generic;
using System.IO;
using AJ.Std.Composition;
using AJ.Std.Composition.Contracts;
using Audience;
using GatewayAttachedControllers;

namespace Controllers.Gateway.Attached {
	public sealed class AttachedControllersInfoSystem : CompositionPartBase, IAttachedControllersInfoSystem {
		public AttachedControllersInfoSystem() {
			AttachedControllerInfos = XmlFactory.GetCounterCorrectionInfosFromXml(Path.Combine(Env.CfgPath, "AttachedControllerInfos.xml"));

			AttachedControllerConfigs = new Dictionary<string, AttachedObjectConfig>();
			foreach (var attachedControllerInfo in AttachedControllerInfos) {
				AttachedControllerConfigs.Add(attachedControllerInfo.Value, attachedControllerInfo.Key);
			}
		}

		private IReadOnlyDictionary<AttachedObjectConfig, string> AttachedControllerInfos { get; }
		private Dictionary<string, AttachedObjectConfig> AttachedControllerConfigs { get; }


		public override void SetCompositionRoot(ICompositionRoot root) {
			// Get all needed c.parts with adding refs to them
		}

		public override string Name => "GatewayAttachedControllers";

		public override void BecameUnused() {
			// Unload all c.parts here
		}

		public string GetAttachedControllerNameByConfig(string gateway, int channel, int type, int number) {
			var key = new AttachedObjectConfig(gateway, channel, type, number);
			if (!AttachedControllerInfos.ContainsKey(key)) {
				throw new AttachedControllerNotFoundException();
			}

			return AttachedControllerInfos[key];
		}

		public AttachedObjectConfig GetAttachedControllerConfigByName(string attachedControllerName) {
			if (!AttachedControllerConfigs.ContainsKey(attachedControllerName)) {
				throw new AttachedControllerNotFoundException();
			}

			return AttachedControllerConfigs[attachedControllerName];
		}
	}

	public class AttachedControllerNotFoundException : Exception { }
}