using System;
using System.Collections.Generic;
using Commands.Contracts;

namespace Commands.Bumiz.CounterSe102 {
  public class GetTarProgCommand : ICounterCommand {
    public ushort Code => 0x0140;

    public string Comment => "Чтение тарифной программы";

    private readonly TariffProgramType _programType;
    private readonly int _monthNumber;
    private readonly byte _zeroBasedStartPointNumber;
    private readonly byte _pointsCount;

    public GetTarProgCommand(TariffProgramType programType, int monthNumber, byte zeroBasedStartPointNumber,
      byte pointsCount) {
      _programType = programType;
      _monthNumber = monthNumber;
      if (zeroBasedStartPointNumber > 15) throw new Exception("Point number must be less than 16");
      if (pointsCount < 1 || pointsCount > 6)
        throw new Exception("Points count must be between 1 and 6 (including 1 and 6)");
      _zeroBasedStartPointNumber = zeroBasedStartPointNumber;
      _pointsCount = pointsCount;
    }

    public byte[] Serialize() {
      var result = new byte[3];
      if (_programType == TariffProgramType.SpecialDay) {
        result[0] = 0;
      }
      else {
        int tp = ((((byte) _programType) - 1) & 0x03) << 4; // 00xx0000;
        int mm = (_monthNumber & 0x0F); // 0000xxxx;

        result[0] = (byte) (tp + mm);
      }

      result[1] = _zeroBasedStartPointNumber;
      result[2] = _pointsCount;
      return result;
    }

    public int AwaitedBytesCount => _pointsCount * 2;

    public GetTarProgResult GetResult(byte[] reply) {
      var result = new GetTarProgResult();
      for (int i = 0; i < reply.Length; i += 2) {
        var halfAnHour = reply[i];
        var tariffProg = reply[i + 1];
        result.Points.Add(new HalfAnHourAndTariffNumber(halfAnHour, tariffProg));
      }

      return result;
    }
  }

  public class GetTarProgResult {
    public readonly List<HalfAnHourAndTariffNumber> Points = new List<HalfAnHourAndTariffNumber>();

    public override string ToString() {
      var result = string.Empty;
      result += "Total points in result = " + Points.Count + Environment.NewLine;
      foreach (var halfAnHourAndTariffNumber in Points) {
        result += halfAnHourAndTariffNumber + Environment.NewLine;
      }

      return result;
    }
  }

  public struct HalfAnHourAndTariffNumber {
    public int HalfAnHourNumber;
    public int TariffNumber;

    public HalfAnHourAndTariffNumber(int halfAnHourNumber, int tariffNumber) {
      HalfAnHourNumber = halfAnHourNumber;
      TariffNumber = tariffNumber;
    }

    public override string ToString() {
      return "HalfAnHourNumber=" + HalfAnHourNumber + "   TarrifNumber=" + TariffNumber;
    }
  }
}