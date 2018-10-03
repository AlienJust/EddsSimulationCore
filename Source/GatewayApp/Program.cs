using System;

namespace GatewayApp {
	internal static class Program {
		static void Main() {
			var compositionRoot = new CompositionRoot();
			Console.ReadKey(true);
			var samplePart = compositionRoot.GetPartByName("xyz");
			if (samplePart == null)
				Console.WriteLine("xyz");
		}
	}
}