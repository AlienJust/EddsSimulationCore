using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AJ.Std.Loggers;
using AJ.Std.Loggers.Contracts;
using AJ.Std.Reflection;
using AJ.Std.Text;
using AJ.Std.Text.Contracts;
using Audience;

namespace Bumiz.Apply.PulseCounterArchiveReader {
  class FilePulseCounterDataStorage : IPulseCounterDataStorage {
    private static readonly ILogger Log = new RelayMultiLogger(true,
      new RelayLogger(Env.GlobalLog,
        new ChainedFormatter(new ITextFormatter[]
          {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})),
      new RelayLogger(new ColoredConsoleLogger(ConsoleColor.Black, ConsoleColor.Green),
        new ChainedFormatter(new ITextFormatter[]
          {new ThreadFormatter(" > ", false, true, false), new DateTimeFormatter(" > ")})));

    private readonly List<IPulseCounterInfo> _infos;
    private readonly Dictionary<string, IObjectData> _namedRecs;
    private readonly string _archivePath;

    public FilePulseCounterDataStorage(IEnumerable<IPulseCounterInfo> infos) {
      _archivePath = Path.Combine(typeof(FilePulseCounterDataStorage).GetAssemblyDirectoryPath(),
        "PulseCountersStorage");

      Log.Log("Bumiz.PulseCountersStorage _archivePath is: " + _archivePath);
      _infos = infos.ToList();
      _namedRecs = new Dictionary<string, IObjectData>();

      foreach (var startupInfo in _infos) {
        _namedRecs.Add(startupInfo.Name,
          new StoredObjectData(new StorageObjectInfo(startupInfo.Name, startupInfo.SetupTime)));
      }

      FillRecsFromFiles();
    }

    private string GetObjectFilePath(string objectName) {
      return Path.Combine(_archivePath, objectName + ".txt");
    }

    private void FillRecsFromFiles() {
      foreach (var namedRec in _namedRecs) {
        var objName = namedRec.Key;
        var objFileName = GetObjectFilePath(objName);
        try {
          if (File.Exists(objFileName))
            using (var sr = new StreamReader(objFileName)) {
              while (!sr.EndOfStream) {
                var line = sr.ReadLine();
                if (line != null) {
                  var parts = line.Split('\t');
                  if (parts.Length == 7 || parts.Length == 8) {
                    var recTime =
                      new DateTime
                      (2000 + int.Parse(parts[0].Substring(0, 2)),
                        int.Parse(parts[0].Substring(2, 2)),
                        int.Parse(parts[0].Substring(4, 2)),
                        int.Parse(parts[0].Substring(6, 2)),
                        int.Parse(parts[0].Substring(8, 2)),
                        0);
                    if (recTime >= namedRec.Value.SetupTime) {
                      var recIsOk = bool.Parse(parts[1]);

                      var pulseCount1 = int.Parse(parts[2]);
                      var pulseCount2 = int.Parse(parts[3]);
                      var pulseCount3 = int.Parse(parts[4]);
                      var status = int.Parse(parts[5]);
                      var statusX = int.Parse(parts[4]);

                      bool isRecoredAdded = namedRec.Value.AddRecord(recTime,
                        new AtomRec {
                          IsRecordCorrect = recIsOk,
                          PulseCount1 = pulseCount1,
                          PulseCount2 = pulseCount2,
                          PulseCount3 = pulseCount3,
                          Status = status,
                          StatusX = statusX
                        });
                      if (!isRecoredAdded) {
                        Log.Log("Record was not added after IObjectData.AddRecord() method call!");
                      }
                    }
                  }
                }
              }
            }
          else Log.Log("File with archive not exist, it will be created during next data saving: " + objFileName);
        }
        catch (Exception ex) {
          Log.Log("Error while filling recs from file " + objFileName);
          Log.Log(ex.ToString());
        }
      }
    }

    public IIntegralData GetIntegralData(string objectName, DateTime upToTime) {
      if (_namedRecs.ContainsKey(objectName))
        return _namedRecs[objectName].GetIntegralData(upToTime);
      throw new Exception("Error getting integral data for object with name: " + objectName + ", no such key (name) was found");
    }

    public List<DateTime> GetMissedTimesUpToTime(string objectName, DateTime nowTime) {
      var result = new List<DateTime>();
      var objInfo = _namedRecs[objectName];

      var curTime = objInfo.SetupTime.AddMinutes(-1.0 * (objInfo.SetupTime.Minute < 30
                                                   ? objInfo.SetupTime.Minute
                                                   : objInfo.SetupTime.Minute -
                                                     30));
      while (curTime < nowTime) {
        if (!objInfo.ContatinsDataForTime(curTime)) {
          result.Add(curTime);
        }

        curTime = curTime.AddMinutes(30);
      }

      return result;
    }

    public DateTime? GetFirstMissedTimeUpToTime(string objectName, DateTime nowTime) {
      var objInfo = _namedRecs[objectName];

      var curTime = objInfo.SetupTime.AddMinutes(-1.0 * (objInfo.SetupTime.Minute < 30
                                                   ? objInfo.SetupTime.Minute
                                                   : objInfo.SetupTime.Minute - 30));
      while (curTime < nowTime) {
        if (!objInfo.ContatinsDataForTime(curTime)) {
          return curTime;
        }

        curTime = curTime.AddMinutes(30);
      }

      return null;
    }

    public void SaveData(string objectName, DateTime time, bool isRecordCorrect, int pulseCount1, int pulseCount2,
      int pulseCount3, int status, int statusX) {
      var objInfo = _namedRecs[objectName];
      var record = new AtomRec {
        IsRecordCorrect = isRecordCorrect,
        PulseCount1 = pulseCount1,
        PulseCount2 = pulseCount2,
        PulseCount3 = pulseCount3,
        Status = status,
        StatusX = status
      };
      if (objInfo.AddRecord(time, record)) {
        var fileLine = (time.Year - 2000).ToString("d2") +
                       time.Month.ToString("d2") +
                       time.Day.ToString("d2") +
                       time.Hour.ToString("d2") +
                       time.Minute.ToString("d2") + "\t" +
                       isRecordCorrect + "\t" +
                       pulseCount1 + "\t" +
                       pulseCount2 + "\t" +
                       pulseCount3 + "\t" +
                       status + "\t" +
                       statusX + "\t" +
                       DateTime.Now.ToString("yyyy.MM.dd-HH:mm:ss");
        using (var sw = File.AppendText(GetObjectFilePath(objectName))) {
          sw.WriteLine(fileLine);
          sw.Close();
        }
      }
      else {
        Log.Log("Record was not added to storage while trying to save data, but it's may be okay");
      }
    }

    public AtomRec? GetAtomicData(string objectName, DateTime certainTime) {
      if (!_namedRecs.ContainsKey(objectName))
        return null;
      try {
        return _namedRecs[objectName].GetAtomicDataForTime(certainTime);
      }
      catch {
        return null;
      }
    }
  }
}