using System;
using System.Collections.Generic;
using System.Xml.Linq;
using AJ.Std.Loggers;
using AJ.Std.Loggers.Contracts;
using AJ.Std.Text;
using AJ.Std.Text.Contracts;
using Audience;

namespace Controllers.Lora {
  internal static class XmlFactory {
    private static readonly ILogger Log = new RelayMultiLogger(true
      , new RelayLogger(Env.GlobalLog
        , new ChainedFormatter(new ITextFormatter[] {
          new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")
        }))
      , new RelayLogger(new ColoredConsoleLogger(ConsoleColor.DarkGreen, Console.BackgroundColor)
        , new ChainedFormatter(new ITextFormatter[] {
          new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")
        })));

    public static IReadOnlyList<LoraControllerInfoSimple> GetObjectsConfigurationsFromXml(string filename) {
      var controllerInfos = new List<LoraControllerInfoSimple>();
      Log.Log("Loading LORA controllers configuration from XML...");

      var docChannels = XDocument.Load(filename);
      {
        var objectsElement = docChannels.Element("LoraObjects");
        if (objectsElement != null) {
          var objectElements = objectsElement.Elements("LoraObject");

          foreach (var objectElement in objectElements) {
            try {
              var objectName = objectElement.Attribute("Name").Value;
              var deviceId = objectElement.Attribute("DeviceId").Value;
              
              var dataTtl = int.Parse(objectElement.Attribute("CurrentDataCacheTtlSeconds").Value);
              

              controllerInfos.Add(new LoraControllerInfoSimple(objectName, deviceId, dataTtl));
              Log.Log("Loaded LORA XML config for object with name " + objectName);
            }
            catch (Exception ex) {
              Log.Log("Error loading LORA XML config: " + ex);
            }
          }
        }
      }
      Log.Log("BUMIZ XML config loaded, objects count is " + controllerInfos.Count);
      return controllerInfos;
    }
  }
}