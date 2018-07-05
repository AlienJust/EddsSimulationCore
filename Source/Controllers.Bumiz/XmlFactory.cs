using System;
using System.Collections.Generic;
using System.Xml.Linq;
using AJ.Std.Loggers;
using AJ.Std.Loggers.Contracts;
using AJ.Std.Text;
using AJ.Std.Text.Contracts;
using Audience;

namespace Controllers.Bumiz {
  internal static class XmlFactory {
    private static readonly ILogger Log = new RelayMultiLogger(
      true,
      new RelayLogger(Env.GlobalLog,
        new ChainedFormatter(new ITextFormatter[]
          {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})),
      new RelayLogger(new ColoredConsoleLogger(ConsoleColor.DarkGreen, Console.BackgroundColor),
        new ChainedFormatter(new ITextFormatter[]
          {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})));

    public static IEnumerable<IBumizControllerInfo> GetBumizObjectInfosFromXml(string filename) {
      var bumizControllerInfos = new List<IBumizControllerInfo>();
      Log.Log("���������� ���������� �� �������� ����� �� XML ����� ������������...");

      var docChannels = XDocument.Load(filename);
      {
        var bumizObjectsElement = docChannels.Element("BumizObjects");
        if (bumizObjectsElement != null) {
          var bumizObjectElements = bumizObjectsElement.Elements("BumizObject");

          foreach (var bumizObjectElement in bumizObjectElements) {
            try {
              var bumizObjectName = bumizObjectElement.Attribute("Name").Value;
              var currentDataCacheTtlSeconds =
                int.Parse(bumizObjectElement.Attribute("CurrentDataCacheTtlSeconds").Value);
              var pulses1Expression = bumizObjectElement.Attribute("Pulse1Correction").Value;
              var pulses2Expression = bumizObjectElement.Attribute("Pulse2Correction").Value;
              var pulses3Expression = bumizObjectElement.Attribute("Pulse3Correction").Value;

              bumizControllerInfos.Add(new BumizControllerInfo(bumizObjectName, currentDataCacheTtlSeconds,
                pulses1Expression, pulses2Expression, pulses3Expression));
              Log.Log("���������� �� ������� ����� " + bumizObjectName + " ����������������");
            }
            catch (Exception ex) {
              Log.Log("�� ������� ���������������� ���������� �� ������� �����");
              Log.Log(ex.ToString());
            }
          }
        }
      }
      Log.Log("���������� �� �������� ����� ���� ��������� �� XML �����, ����� ��������: " +
              bumizControllerInfos.Count);
      return bumizControllerInfos;
    }
  }
}