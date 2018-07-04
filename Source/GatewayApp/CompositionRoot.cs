using System;
using System.Collections.Generic;
using System.Composition;
using System.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using AJ.Std.Composition.Contracts;
using AJ.Std.Loggers;
using AJ.Std.Loggers.Contracts;
using AJ.Std.Reflection;
using AJ.Std.Text;
using AJ.Std.Text.Contracts;

using Audience;
namespace GatewayApp {
	class CompositionRoot : ICompositionRoot {
		private static readonly ILogger Log = new RelayMultiLogger(true, new RelayLogger(Env.GlobalLog, new ChainedFormatter(new ITextFormatter[] { new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ") })), new RelayLogger(new ColoredConsoleLogger(ConsoleColor.DarkRed, Console.BackgroundColor), new ChainedFormatter(new ITextFormatter[] { new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ") })));

		[ImportMany]
		public IEnumerable<ICompositionPart> _compositionParts { get; set; }

		public CompositionRoot() {
			Log.Log("CompositionRoot constructor called");
			var dllFiles = Directory.GetFiles(typeof(CompositionRoot).GetAssemblyDirectoryPath(), "*.dll").ToList();
			var assemblies = dllFiles.Select(p => {
				Console.WriteLine(p);
				return AssemblyLoadContext.Default.LoadFromAssemblyPath(p);
			}).Where(x => x != null).ToList();

			Log.Log("Assemblies count = " + assemblies.Count);
			foreach (var assembly in assemblies) {
				Log.Log(assembly.FullName);
			}

			var configuration = new ContainerConfiguration().WithAssemblies(assemblies);
			using (var container = configuration.CreateContainer()) {
				_compositionParts = container.GetExports<ICompositionPart>();
			}


			//_compositionParts = exportedTypes.ToList();

			try {
				foreach (var compositionPart in _compositionParts) {
					try {
						Log.Log("Инициализация композиционной части " + compositionPart.Name);
						compositionPart.SetCompositionRoot(this);
					}
					catch (Exception ex) {
						Log.Log("Ошибка при задании ссылки на CompositionRoot, исключение: " + ex);
					}
				}

				Log.Log("Загружено композиционных частей: " + _compositionParts.Count());
			}
			catch (Exception ex) {
				Log.Log("Загружено композиционных частей: не удалось провести композицию программы. Исключение: " + ex);
			}
			Log.Log("CompositionRoot constructor complete");
		}

		public ICompositionPart GetPartByName(string partName) {
			return _compositionParts.First(cp => cp.Name == partName);
		}
	}
}
