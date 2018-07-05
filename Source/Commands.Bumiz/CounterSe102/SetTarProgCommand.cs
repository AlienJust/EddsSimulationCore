using System;
using System.Collections.Generic;
using Commands.Contracts;

namespace Commands.Bumiz.CounterSe102 {
  public class SetTarProgCommand : ICounterCommand {
    public ushort Code => 0x0141;

    public string Comment => "Запись тарифной программы";

    private readonly TariffProgramType _programType;
    private readonly int _monthNumber;
    private readonly byte _zeroBasedStartPointNumber;
    private readonly List<HalfAnHourAndTariffNumber> _points;

    public SetTarProgCommand(TariffProgramType programType, int monthNumber, byte zeroBasedStartPointNumber,
      List<HalfAnHourAndTariffNumber> points) {
      _programType = programType;
      _monthNumber = monthNumber;
      if (zeroBasedStartPointNumber > 15) throw new Exception("Point number must be less than 16");
      if (points.Count < 1 || points.Count > 6)
        throw new Exception("Points count must be between 1 and 6 (including 1 and 6)");
      _zeroBasedStartPointNumber = zeroBasedStartPointNumber;
      _points = points;
    }

    public byte[] Serialize() {
      var result = new byte[3 + 2 * _points.Count];
      if (_programType == TariffProgramType.SpecialDay) {
        result[0] = 0;
      }
      else {
        int tp = (((byte) _programType - 1) & 0x03) << 4; // 00xx0000;
        int mm = _monthNumber & 0x0F; // 0000xxxx;

        result[0] = (byte) (tp + mm);
      }

      result[1] = _zeroBasedStartPointNumber;
      result[2] = (byte) _points.Count;
      for (int i = 0; i < _points.Count; ++i) {
        result[3 + i * 2] = (byte) _points[i].HalfAnHourNumber;
        result[3 + i * 2 + 1] = (byte) _points[i].TariffNumber;
      }

      return result;
    }

    public int AwaitedBytesCount => 0;
  }
}