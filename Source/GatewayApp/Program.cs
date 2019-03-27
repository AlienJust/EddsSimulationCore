using System;
using System.Collections.Generic;
using System.Linq;
using AJ.Std.Composition.Contracts;
using Controllers.AttachedVirtual;
using Controllers.Gateway;
using Controllers.Gateway.Attached;
using Controllers.Lora;
using PollServiceProxy;

namespace GatewayApp {
	internal static class Program {
		static void Main() {
			var crs = new CrS();
			crs.Parts.Add(new LoraControllersSubSystem());
			crs.Parts.Add(new InteleconGateway());
			crs.Parts.Add(new GatewayControllersSubSystem());
			crs.Parts.Add(new VirtualControllersSystem());
			crs.Parts.Add(new AttachedControllersInfoSystem());
			//var compositionRoot = new CompositionRoot();
			
			foreach (var compositionPart in crs.Parts) {
				compositionPart.SetCompositionRoot(crs);
			}
			Console.ReadKey(true);
		}
	}

	class CrS : ICompositionRoot {
		public CrS() {
			Parts = new List<ICompositionPart>();
		}
		public List<ICompositionPart> Parts { get; }
		
		public ICompositionPart GetPartByName(string partName) {
			Console.WriteLine("Called for part " + partName);
			return Parts.First(p => p.Name == partName);
		}
	}
}