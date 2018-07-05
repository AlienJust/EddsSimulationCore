using System;
using Commands.Contracts;

namespace Commands.Bumiz.Intelecon {
  // TODO:
  public class ReadArchiveRecordServiceCommand3 : IInteleconCommand {
    public byte Code => 0x09;

    public string Comment => "Чтение данных архива через 9";

    public int RecordNumber { get; }

    public DateTime RequestedTime { get; }

    public byte RecordPart => 2;

    public ReadArchiveRecordServiceCommand3(DateTime time) {
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


    public IAdvancedArchiveResult3 GetThirdResult(byte[] reply) {
      int hour = (reply[1] & 0xF8) >> 3;
      int minute = ((reply[1] & 0x07) << 3) + ((reply[0] & 0xE0) >> 5);
      int second = reply[0] & 0x1F;
      var dt = new TimeSpan(hour, minute, second);

      int hw1Low = reply[2];
      int hw1High = (reply[3] & 0x0F) << 8;
      var hw2Low = (reply[3] & 0xF0) >> 4;
      int hw2High = reply[4] << 4;

      var hw1 = hw1Low + hw1High;
      var hw2 = hw2Low + hw2High;

      int cw1Low = reply[5];
      int cw1High = (reply[6] & 0x0F) << 8;
      var cw2Low = (reply[6] & 0xF0) >> 4;
      int cw2High = reply[7] << 4;

      var cw1 = cw1Low + cw1High;
      var cw2 = cw2Low + cw2High;

      int gg1Low = reply[8];
      int gg1High = (reply[9] & 0x0F) << 8;
      var gg2Low = (reply[9] & 0xF0) >> 4;
      int gg2High = reply[10] << 4;

      var g1 = gg1Low + gg1High;
      var g2 = gg2Low + gg2High;

      var xstatus = reply[11] + reply[12] * 256;
      var r45 = reply[13];
      var r46 = reply[14];
      var r47 = reply[15];

      return new AdvancedArchiveResult3Simple {
        Time = dt,
        ColdWater1 = cw1,
        ColdWater2 = cw2,
        Gas1 = g1,
        Gas2 = g2,
        HotWater1 = hw1,
        HotWater2 = hw2,
        Xstatus = xstatus,

        R45 = r45,
        R46 = r46,
        R47 = r47
      };
    }
  }
}