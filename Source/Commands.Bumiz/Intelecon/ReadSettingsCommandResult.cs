using System;

namespace Commands.Bumiz.Intelecon {
  public struct ReadSettingsCommandResult {
    public ReadSettingsCommandResult(ushort inteleconAddr, byte controlWord, byte thresholdCurrent, byte thresholdTime,
      byte protectionTimeout, byte autoPowerOnTimeout)
      : this() {
      InteleconAddr = inteleconAddr;
      ControlWord = controlWord;
      ThresholdCurrent = thresholdCurrent;
      ThresholdTime = thresholdTime;
      ProtectionTimeout = protectionTimeout;
      AutoPowerOnTimeout = autoPowerOnTimeout;
    }


    public byte AutoPowerOnTimeout { get; }

    public byte ProtectionTimeout { get; }

    public byte ThresholdTime { get; }

    public byte ThresholdCurrent { get; }

    public byte ControlWord { get; }

    public ushort InteleconAddr { get; }

    public override string ToString() {
      string result = string.Empty;
      result += "����� ���������: \t" + InteleconAddr + Environment.NewLine;
      result += "����������� �����: \t" + ControlWord + Environment.NewLine;
      result += "����� ���������� �� ����: \t" + ThresholdCurrent + Environment.NewLine;
      result += "����� ����������: \t" + ThresholdTime + Environment.NewLine;
      result += "������� ������: \t" + ProtectionTimeout + Environment.NewLine;
      result += "������� ��������������� ����������: \t" + AutoPowerOnTimeout + Environment.NewLine;
      return result;
    }
  }
}