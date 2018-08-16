using System;
using System.Collections.Generic;
using System.Xml.Linq;
using AJ.Std.Loggers;
using AJ.Std.Loggers.Contracts;
using AJ.Std.Text;
using AJ.Std.Text.Contracts;
using Audience;

namespace Bumiz.Apply.PulseCounterArchiveReader {
  internal static class XmlFactory {
    private static readonly ILogger Log = new RelayMultiLogger(true,
      new RelayLogger(Env.GlobalLog,
        new ChainedFormatter(new ITextFormatter[]
          {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})),
      new RelayLogger(new ColoredConsoleLogger(ConsoleColor.Green, Console.BackgroundColor),
        new ChainedFormatter(new ITextFormatter[]
          {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})));

    public static Dictionary<string, IPulseCounterInfo> GetCountersFromXml(string filename) {
      var counterInfos = new Dictionary<string, IPulseCounterInfo>();
      Log.Log("Loading BUMIZ pulse counters configs from XML...");
      var docChannels = XDocument.Load(filename);
      {
        var rootNode = docChannels.Element("PulseCounters");
        if (rootNode != null) {
          var counters = rootNode.Elements("PulseCounter");
          foreach (var comChannel in counters) {
            try {
              var counterName = comChannel.Attribute("Name").Value;

              var parts = comChannel.Attribute("SetupAt").Value.Split('-');
              var setupDateTime = new DateTime
              (int.Parse(parts[0]),
                int.Parse(parts[1]),
                int.Parse(parts[2]),
                int.Parse(parts[3]),
                int.Parse(parts[4]),
                0);

              counterInfos.Add(counterName, new PulseCounterInfo(counterName, setupDateTime));
              Log.Log("Loaded config for counter with name: " + counterName + " it's setup date and time are: " +
                      setupDateTime.ToString("yyyy.MM.dd-HH:mm"));
            }
            catch (Exception ex) {
              Log.Log("Error during loading XML configuration for some single BUMIZ pulse counter: " + ex);
            }
          }
        }
      }
      Log.Log("BUMIZ pulse counters configurations were loaded from XML, loaded items count is: " + counterInfos.Count);
      return counterInfos;
    }
  }
}