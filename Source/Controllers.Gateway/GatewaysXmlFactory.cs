using System;
using System.Collections.Generic;
using System.Xml.Linq;
using AJ.Std.Loggers;
using AJ.Std.Loggers.Contracts;
using AJ.Std.Text;
using AJ.Std.Text.Contracts;
using Audience;

namespace Controllers.Gateway {
	internal static class GatewayesXmlFactory {
		private static readonly ILogger Log = new RelayMultiLogger(true, new RelayLogger(Env.GlobalLog, new ChainedFormatter(new ITextFormatter[] { new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ") })), new RelayLogger(new ColoredConsoleLogger(ConsoleColor.DarkGreen, Console.BackgroundColor), new ChainedFormatter(new ITextFormatter[] { new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ") })));
		public static IEnumerable<IGatewayControllerInfo> GetGatewaysConfigFromXml(string filename) {
			var gatewayControllerInfos = new List<IGatewayControllerInfo>();
			Log.Log("���������� ���������� � ������ �� XML ����� ������������...");

			var docChannels = XDocument.Load(filename);
			{
				var gatewaysElement = docChannels.Element("Gateways");
				if (gatewaysElement != null) {
					var gatewayElements = gatewaysElement.Elements("Gateway");
					foreach (var gatewayElement in gatewayElements) {
						try {
							var gatewayName = gatewayElement.Attribute("Name").Value;
							gatewayControllerInfos.Add(new GatewayControllerInfo(gatewayName));
							Log.Log("���������� � ����� " + gatewayName + " ����������������");
						}
						catch (Exception ex) {
							Log.Log("�� ������� ���������������� ���������� � �����");
							Log.Log(ex.ToString());
						}
					}
				}
			}
			Log.Log("���������� � ������ ���� ��������� �� XML �����, ����� ������: " + gatewayControllerInfos.Count);
			return gatewayControllerInfos;
		}
	}
}