using System;
using System.Linq;

namespace Bumiz.Apply.PulseCounterArchiveReader {
  class StoredObjectData : IObjectData {
    private readonly StorageObjectInfo _storageObjectInfo;

    public StoredObjectData(StorageObjectInfo storageObjectInfo) {
      _storageObjectInfo = storageObjectInfo;
    }

    /// <summary>
    /// ���������� ������, ���� ������ ���� ��������� � ����, ���� ������ ��� ���� � ������� 
    /// </summary>
    /// <param name="time"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public bool AddRecord(DateTime time, AtomRec data) {
      if (_storageObjectInfo.FileRecords.ContainsKey(time))
        return false;
      _storageObjectInfo.FileRecords.Add(time, data);
      return true;
    }


    public IIntegralData GetIntegralData(DateTime upToTime) {
      if (upToTime < SetupTime)
        throw new Exception("������, ����� ������� ������ (" + upToTime.ToString("yyyy.MM.dd-HH:mm") +
                            ") ������ ������� ���������� ������� �� ���� (" + SetupTime.ToString("yyyy.MM.dd-HH:mm") +
                            ")");
      var upToTimeRecords = _storageObjectInfo.FileRecords.Where(kvp => kvp.Key <= upToTime && kvp.Key >= SetupTime)
        .Select(kvp => kvp.Value).ToList();
      var recordsCount = upToTimeRecords.Count;
      var supposedRecordsCount = (int) ((upToTime - SetupTime).TotalMinutes / 30.0) + 1;
      if (recordsCount != supposedRecordsCount)
        throw new Exception("���������� �������� ���������� ��������� ������, �.�. ����� ����������� � ��������� (" +
                            recordsCount + ")�� ����� ��������������� ����� ����������� (" + supposedRecordsCount +
                            ")");

      var correctRecordsCount = upToTimeRecords.Count(r => r.IsRecordCorrect);
      var incorrectRecordsCount = recordsCount - correctRecordsCount;

      var impulsesCount1 = upToTimeRecords.Sum(r => r.PulseCount1);
      var impulsesCount2 = upToTimeRecords.Sum(r => r.PulseCount2);
      var impulsesCount3 = upToTimeRecords.Sum(r => r.PulseCount3);

      return new IntegralData(impulsesCount1, impulsesCount2, impulsesCount3, recordsCount, correctRecordsCount,
        incorrectRecordsCount, supposedRecordsCount);
    }

    public DateTime SetupTime => _storageObjectInfo.SetupTime;

    public string ObjectName => _storageObjectInfo.ObjectName;

    public bool ContatinsDataForTime(DateTime time) {
      return _storageObjectInfo.FileRecords.ContainsKey(time);
    }

    public AtomRec GetAtomicDataForTime(DateTime time) {
      return _storageObjectInfo.FileRecords[time];
    }

    public override string ToString() {
      return "[" + ObjectName + "] > FileRecordsCount=" + _storageObjectInfo.FileRecords;
    }
  }
}