using System;
using System.Collections.Generic;
using System.Net;
using System.Xml.Linq;
using AJ.Std.Loggers;
using AJ.Std.Loggers.Contracts;
using AJ.Std.Text;
using AJ.Std.Text.Contracts;
using Audience;
using ScadaClient.Udp;

namespace PollServiceProxy {
  internal static class XmlFactory {
    private static readonly ILogger Log = new RelayMultiLogger(true,
      new RelayLogger(Env.GlobalLog,
        new ChainedFormatter(new ITextFormatter[]
          {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})),
      new RelayLogger(new ColoredConsoleLogger(ConsoleColor.Blue, Console.BackgroundColor),
        new ChainedFormatter(new ITextFormatter[]
          {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})));

    public static Dictionary<string, INamedScadaLink> GetScadaLinksFromXml(string filename) {
      var scadaLinks = new Dictionary<string, INamedScadaLink>();
      Log.Log("Getting SCADA link infos from XML file: " + filename);
      var docServers = XDocument.Load(filename);
      {
        var rootNode = docServers.Element("Servers");
        if (rootNode != null) {
          var udpServers = rootNode.Elements("UdpServer");
          foreach (var udpServer in udpServers) {
            try {
              var linkLabel = udpServer.Attribute("Label").Value;
              var ip = udpServer.Attribute("Ip").Value;
              var port = int.Parse(udpServer.Attribute("Port").Value);
              var localPort = int.Parse(udpServer.Attribute("LocalPort").Value);
              var connectionDropPeriodMs = int.Parse(udpServer.Attribute("ConnectionDropPeriodMs").Value);
              //var client = new NamedScadaLinkSimple(linkLabel, new AsyncUdpClient(ip, port, localPort, connectionDropPeriodMs));
              var client =
                new NamedScadaLinkSimple(linkLabel, new QueuedScadaClient(IPAddress.Parse(ip), port, localPort));
              scadaLinks.Add(linkLabel, client);
              Log.Log("SCADA link added: " + ip + ":" + port + " (local UDP port is " + localPort +
                      ", connection drop period = " + connectionDropPeriodMs +
                      "ms), and is now known as " + linkLabel);
            }
            catch (Exception ex) {
              Log.Log("Exception while load information about SCADA link from XML");
              Log.Log(ex.ToString());
            }
          }
        }
      }
      Log.Log("SCADA links was loaded, total count is " + scadaLinks.Count);

      return scadaLinks;
    }

    public static Dictionary<string, IScadaObjectInfo> GetScadaObjectsFromXml(string filename) {
      var scadaObjects = new Dictionary<string, IScadaObjectInfo>();
      Log.Log("Getting SCADA objects from XML file " + filename);
      var docObjects = XDocument.Load(filename);
      {
        var rootNode = docObjects.Element("Objects");
        if (rootNode != null) {
          var objectNodes = rootNode.Elements("Object");
          foreach (var objNode in objectNodes) {
            try {
              var objLabel = objNode.Attribute("Name").Value;
              var sendMicroPackets = bool.Parse(objNode.Attribute("SendMicroPackets").Value);
              var targetsNode = objNode.Element("Targets");
              var serverNodes = targetsNode.Elements("UdpScadaLink");
              var scadaLinks = new List<IScadaAddress>();
              foreach (var serverNode in serverNodes) {
                var targetServerLabel = serverNode.Attribute("Label").Value;
                var netAddress = int.Parse(serverNode.Attribute("Address").Value);
                scadaLinks.Add(new ScadaAddress {LinkName = targetServerLabel, NetAddress = netAddress});
              }

              scadaObjects.Add(objLabel,
                new ScadaObjectInfo {
                  Name = objLabel,
                  ScadaAddresses = scadaLinks,
                  SendMicroPackets = sendMicroPackets
                });
              Log.Log("SCADA object was added: scada links count is " + scadaLinks.Count +
                      ", and is is now known as " + objLabel);
            }
            catch (Exception ex) {
              Log.Log("Exception while load information about SCADA object from XML");
              Log.Log(ex.ToString());
            }
          }
        }
      }
      Log.Log("SCADA objects was loaded, total count is  = " + scadaObjects.Count);
      return scadaObjects;
    }

    public static int GetMicroPacketSendingIntervalMsFromXml(string filename) {
      return int.Parse(XDocument.Load(filename).Element("Settings").Element("MicroPacketSending")
        .Attribute("IntervalMs").Value);
    }
  }
}