using System;
using Commands.Contracts;

namespace Commands.Bumiz.Intelecon {
  // TODO:
  public class ReadArchiveRecordServiceCommand2 : IInteleconCommand {
    public byte Code => 0x09;

    public string Comment => "Чтение данных архива через 9";

    public int RecordNumber { get; }

    public DateTime RequestedTime { get; }

    public byte RecordPart => 1;

    public ReadArchiveRecordServiceCommand2(DateTime time) {
      RequestedTime = time;
      RecordNumber =
        ((time.Year - 2000) * 17856 + //* 2 * 12 * 31 * 24 +
         (time.Month - 1) * 1488 + //*2*31*24 +
         (time.Day - 1) * 48 + //*2*24 +
         time.Hour * 2 +
         (time.Minute < 30 ? 0 : 1)) % 4096;
    }

    public byte[] Serialize() {
      var result = new byte[4];
      result[0] = 0x05;
      result[1] = (byte) (RecordNumber & 0xFF);
      result[2] = (byte) ((RecordNumber & 0xFF00) >> 8);
      result[3] = RecordPart;
      return result;
    }


    public IAdvancedArchiveResult2 GetFirstResult(byte[] reply) {
      var ibMean = reply[0];
      var icMean = reply[1];
      var ibPeak = reply[2];
      var icPeak = reply[3];

      var ubMean = reply[4];
      var ucMean = reply[5];
      var ubPeak = reply[6];
      var ucPeak = reply[7];

      var t3 = reply[8] + reply[9] * 256 + reply[10] * 65536 + reply[11] * 16777216;
      var t4 = reply[12] + reply[13] * 256 + reply[14] * 65536 + reply[15] * 16777216;

      return new AdvancedArchiveResult2Simple {
        IbMean = ibMean,
        IbPeak = ibPeak,
        IcMean = icMean,
        IcPeak = icPeak,
        T3 = t3,
        T4 = t4,
        UbMean = ubMean,
        UbPeak = ubPeak,
        UcMean = ucMean,
        UcPeak = ucPeak,
      };
    }
  }
}