using System;
using System.Collections.Generic;
using System.Xml.Linq;
using AJ.Std.Loggers;
using AJ.Std.Loggers.Contracts;
using AJ.Std.Text;
using AJ.Std.Text.Contracts;
using Audience;
using Controllers.Gateway.Attached;

namespace GatewayAttachedControllers {
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

    public static IReadOnlyDictionary<AttachedObjectConfig, string> GetCounterCorrectionInfosFromXml(string filename) {
      var attachedControllerInfos = new Dictionary<AttachedObjectConfig, string>();
      Log.Log("Gateway.Attached > loading XML config");

      var docChannels = XDocument.Load(filename);
      {
        var attachedControllerInfosElement = docChannels.Element("AttachedControllerInfos");
        if (attachedControllerInfosElement != null) {
          var attachedControllerInfoElements = attachedControllerInfosElement.Elements("AttachedControllerInfo");
          foreach (var attachedControllerInfoElement in attachedControllerInfoElements) {
            try {
              var attachedControllerGateway = attachedControllerInfoElement.Attribute("Gateway").Value;
              var attachedControllerChannel = int.Parse(attachedControllerInfoElement.Attribute("Channel").Value);
              var attachedControllerType = int.Parse(attachedControllerInfoElement.Attribute("Type").Value);
              var attachedControllerNumber = int.Parse(attachedControllerInfoElement.Attribute("Number").Value);
              var attachedControllerName = attachedControllerInfoElement.Attribute("Name").Value;

              attachedControllerInfos.Add(
                new AttachedObjectConfig(attachedControllerGateway, attachedControllerChannel, attachedControllerType
                  , attachedControllerNumber), attachedControllerName);
              Log.Log("AttachedControllerInfo " + attachedControllerName + " loaded from XML");
            }
            catch (Exception ex) {
              Log.Log("Error during loading XML attached config: " + ex);
            }
          }
        }
      }
      Log.Log("Loading attachedControllerInfos from XML is complete, attached infos count is: " +
              attachedControllerInfos.Count);
      return attachedControllerInfos;
    }
  }
}