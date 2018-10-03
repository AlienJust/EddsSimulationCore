using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AJ.Std.Composition.Contracts;
using AJ.Std.Loggers;
using AJ.Std.Loggers.Contracts;
using AJ.Std.Reflection;
using AJ.Std.Text;
using AJ.Std.Text.Contracts;
using Audience;

namespace GatewayApp {
	class CompositionRoot : ICompositionRoot {
		private static readonly ILogger Log = new RelayMultiLogger(true
			, new RelayLogger(Env.GlobalLog
				, new ChainedFormatter(new ITextFormatter[] {
					new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")
				}))
			, new RelayLogger(new ColoredConsoleLogger(ConsoleColor.Red, Console.BackgroundColor)
				, new ChainedFormatter(new ITextFormatter[] {
					new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")
				})));

		//[ImportMany] public IEnumerable<ICompositionPart> _compositionParts { get; set; }
		private readonly List<ICompositionPart> _compositionParts;


		public CompositionRoot() {
			Log.Log("CompositionRoot constructor called");
			_compositionParts = new List<ICompositionPart>();
			var dllFiles = Directory.GetFiles(typeof(CompositionRoot).GetAssemblyDirectoryPath(), "*.dll").ToList();

			//var loaders = new List<PluginLoader>();
			var assemblies = new List<Assembly>();

			foreach (var dllFile in dllFiles) {
				try {
					assemblies.Add(AssemblyLoader.LoadFromAssemblyPath(dllFile));
					//loaders.Add(PluginLoader.CreateFromAssemblyFile(dllFile, new[] {typeof(ICompositionPart)}));
					Log.Log("Loaded assembly from " + dllFile);
				}
				catch (Exception e) {
					Console.WriteLine(e);
				}
			}

			/*
      var configuration = new ContainerConfiguration().WithAssemblies(assemblies);
      using (var container = configuration.CreateContainer()) {
        _compositionParts = container.GetExports<ICompositionPart>();
      }*/
			foreach (var assembly in assemblies) {
				var types = assembly.GetTypes().Where(t => typeof(ICompositionPart).IsAssignableFrom(t) && !t.IsAbstract);
				foreach (var t in types) {
					var part = (ICompositionPart)Activator.CreateInstance(t);
					_compositionParts.Add(part);
				}
			}
			/*
      foreach (var loader in loaders) {
        foreach (var pluginType in loader.LoadDefaultAssembly().GetTypes()
          .Where(t => typeof(ICompositionPart).IsAssignableFrom(t) && !t.IsAbstract)) {
          // This assumes the implementation of IPlugin has a parameterless constructor
          ICompositionPart plugin = (ICompositionPart) Activator.CreateInstance(pluginType);

          Console.WriteLine($"Created plugin instance '{plugin.Name}'.");
          _compositionParts.Add(plugin);
        }
      }*/


			try {
				foreach (var compositionPart in _compositionParts) {
					try {
						Log.Log("Инициализация композиционной части " + compositionPart.Name + " (" +
										compositionPart.GetType().FullName + ")");
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