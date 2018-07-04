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
		private static readonly ILogger Log = new RelayMultiLogger(true, new RelayLogger(Env.GlobalLog, new ChainedFormatter(new ITextFormatter[] { new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ") })), new RelayLogger(new ColoredConsoleLogger(ConsoleColor.Blue, Console.BackgroundColor), new ChainedFormatter(new ITextFormatter[] { new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ") })));

		public static Dictionary<string, INamedScadaLink> GetScadaLinksFromXml(string filename) {
			var scadaLinks = new Dictionary<string, INamedScadaLink>();
			Log.Log("Построение связей с серверами согласно XML конфигурации...");
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
							var client  = new NamedScadaLinkSimple(linkLabel, new QueuedScadaClient(IPAddress.Parse(ip), port, localPort));
							scadaLinks.Add(linkLabel, client);
							Log.Log("Связь построена, точка соединения: " + ip + ":" + port + " (локальный UDP порт " + localPort + ", период принудительного разрыва связи = " + connectionDropPeriodMs + "мс), теперь связь известна в системе как " + linkLabel);
						}
						catch (Exception ex) {
							Log.Log("не удалось инициализировать связь с сервером из XML");
							Log.Log(ex.ToString());
						}
					}
				}
			}
			Log.Log("Связи с серверами были инициализированы, всего связей: " + scadaLinks.Count);

			return scadaLinks;
		}

		public static Dictionary<string, IScadaObjectInfo> GetScadaObjectsFromXml(string filename) {
			var scadaObjects = new Dictionary<string, IScadaObjectInfo>();
			Log.Log("Инициализация объектов Интелекон согласно XML конфигурации...");
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
							scadaObjects.Add(objLabel, new ScadaObjectInfo { Name = objLabel, ScadaAddresses = scadaLinks, SendMicroPackets = sendMicroPackets});
							Log.Log("Объект инициализирован: число связей объекта =" + scadaLinks.Count + ", теперь объект известен в системе как " + objLabel);
						}
						catch (Exception ex) {
							Log.Log("Не удалось инициализировать объект");
							Log.Log(ex.ToString());
						}
					}
				}
			}
			Log.Log("Инициализация объектов Интелекон завершена, число объектов = " + scadaObjects.Count);
			return scadaObjects;
		}

		public static int GetMicroPacketSendingIntervalMsFromXml(string filename) {
			return int.Parse(XDocument.Load(filename).Element("Settings").Element("MicroPacketSending").Attribute("IntervalMs").Value);
		}
	}
}