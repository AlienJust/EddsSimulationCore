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
			Log.Log("���������� ������ � ��������� �������� XML ������������...");
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
							Log.Log("����� ���������, ����� ����������: " + ip + ":" + port + " (��������� UDP ���� " + localPort + ", ������ ��������������� ������� ����� = " + connectionDropPeriodMs + "��), ������ ����� �������� � ������� ��� " + linkLabel);
						}
						catch (Exception ex) {
							Log.Log("�� ������� ���������������� ����� � �������� �� XML");
							Log.Log(ex.ToString());
						}
					}
				}
			}
			Log.Log("����� � ��������� ���� ����������������, ����� ������: " + scadaLinks.Count);

			return scadaLinks;
		}

		public static Dictionary<string, IScadaObjectInfo> GetScadaObjectsFromXml(string filename) {
			var scadaObjects = new Dictionary<string, IScadaObjectInfo>();
			Log.Log("������������� �������� ��������� �������� XML ������������...");
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
							Log.Log("������ ���������������: ����� ������ ������� =" + scadaLinks.Count + ", ������ ������ �������� � ������� ��� " + objLabel);
						}
						catch (Exception ex) {
							Log.Log("�� ������� ���������������� ������");
							Log.Log(ex.ToString());
						}
					}
				}
			}
			Log.Log("������������� �������� ��������� ���������, ����� �������� = " + scadaObjects.Count);
			return scadaObjects;
		}

		public static int GetMicroPacketSendingIntervalMsFromXml(string filename) {
			return int.Parse(XDocument.Load(filename).Element("Settings").Element("MicroPacketSending").Attribute("IntervalMs").Value);
		}
	}
}