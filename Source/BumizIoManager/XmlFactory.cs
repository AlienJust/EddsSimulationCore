using System;
using System.Collections.Generic;
using System.Xml.Linq;
using AJ.Std.Loggers;
using AJ.Std.Loggers.Contracts;
using AJ.Std.Text;
using AJ.Std.Text.Contracts;
using Audience;
using BumizIoManager.Contracts;
using BumizNetwork;
using BumizNetwork.Contracts;
using BumizNetwork.SerialChannel;

namespace BumizIoManager {
  internal static class XmlFactory {
    private static readonly ILogger Log = new RelayMultiLogger(true,
      new RelayLogger(Env.GlobalLog,
        new ChainedFormatter(new ITextFormatter[]
          {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})),
      new RelayLogger(new ColoredConsoleLogger(ConsoleColor.DarkMagenta, ConsoleColor.Black),
        new ChainedFormatter(new ITextFormatter[]
          {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})));

    public static Dictionary<string, IMonoChannel> GetChannelsFromXml(string filename) {
      var channels = new Dictionary<string, IMonoChannel>();

      //TODO: make static factory for each iface type
      Log.Log("���������� ������� ����� �� XML ����� ������������...");
      var docChannels = XDocument.Load(filename);
      {
        var rootNode = docChannels.Element("BumizChannels");
        if (rootNode != null) {
          var comChannels = rootNode.Elements("ComPortChannel");
          foreach (var comChannel in comChannels) {
            try {
              var channelLabel = comChannel.Attribute("Label").Value;
              var comName = comChannel.Attribute("PortName").Value;
              var baudRate = int.Parse(comChannel.Attribute("BaudRate").Value);
              var onlineCheckTimeSeconds = int.Parse(comChannel.Attribute("OnlineCheckTimeSeconds").Value);
              var checkNetAddress = bool.Parse(comChannel.Attribute("CheckNetAddress").Value);

              Log.Log("����� ������ �� XML = " + channelLabel);
              Log.Log("�������� COM ����� �� XML = " + comName);
              Log.Log("�������� ������ ����� �� XML = " + baudRate);

              var subChannel = new SerialChannelSimple(comName, baudRate, 15);
              Log.Log("����� ����������������� ������ SerialChannelSimple �������� XML ������ �������");

              var channel = new BumizAdvancedNetwork(subChannel, null, onlineCheckTimeSeconds, checkNetAddress);
              Log.Log("BumizAdvancedNetwork ������������ �� SerialChannelSimple ���� ������� �������");

              channels.Add(channelLabel, channel);
              Log.Log("����� ���������������: ����=" + comName + ", ��������=" + baudRate +
                      ", � ������� ����� ������ �������� ���: " + channelLabel);
            }
            catch (Exception ex) {
              Log.Log("�� ������� ���������������� ���������������� ����� �����");
              Log.Log(ex.ToString());
            }
          }
        }
      }
      Log.Log("������ ���������, ���������� ������� = " + channels.Count);
      return channels;
    }

    public static Dictionary<string, IBumizObjectInfo> GetObjectsFromXml(string filename) {
      var objects = new Dictionary<string, IBumizObjectInfo>();
      //_clients = new Dictionary<string, IScadaClient>();
      //_scadaObjects = new Dictionary<string, IScadaObjectInfo>();

      //TODO: make static factory for each iface type
      Log.Log("������������� �������� ����� �������� XML ������������...");
      var docChannels = XDocument.Load(filename);
      {
        var rootNode = docChannels.Element("Objects");
        if (rootNode != null) {
          var objectNodes = rootNode.Elements("Object");
          foreach (var objNode in objectNodes) {
            try {
              var objectName = objNode.Attribute("Label").Value;
              var adrNode = objNode.Element("Address");
              var channelName = adrNode.Attribute("Channel").Value;
              var addressTypeStr = adrNode.Attribute("Type").Value.ToLower();

              NetIdRetrieveType addressType;
              switch (addressTypeStr) {
                case "sn":
                  addressType = NetIdRetrieveType.SerialNumber;
                  break;
                case "ia":
                  addressType = NetIdRetrieveType.InteleconAddress;
                  break;
                case "oldsn":
                  addressType = NetIdRetrieveType.OldProtocolSerialNumber;
                  break;
                default:
                  throw new Exception("�� �������������� ��� ���������");
              }

              var addressValue = int.Parse(adrNode.Attribute("Value").Value);
              var timeout = int.Parse(adrNode.Attribute("Timeout").Value);

              var objectInfo = new BumizObjectInfo(objectName, channelName,
                new ObjectAddress(addressType, (ushort) addressValue), timeout);
              objects.Add(objectName, objectInfo);
              Log.Log("������ ����� ��������: " + objectName + "@" + channelName + ":[" + objectInfo.Address + "]" +
                      ", �������=" + timeout);
            }
            catch (Exception ex) {
              Log.Log("�� ������� ��������� ������ ����� �� XML");
              Log.Log(ex.ToString());
            }
          }
        }
      }
      Log.Log("������� ����� ����������������, ���������� �������� = " + objects.Count);
      return objects;
    }
  }
}