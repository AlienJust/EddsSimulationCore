using System;

namespace Commands.Bumiz.Intelecon {
  public struct PollCommandResult {
    public byte Status;
    public byte To;

    public byte PhaseAcurrent;
    public byte PhaseBcurrent;
    public byte PhaseCcurrent;
    public double PowerT1;
    public double PowerT2;

    public PollCommandResult(byte status, byte to, byte ia, byte ib, byte ic, double t1, double t2) {
      Status = status;
      To = to;
      PhaseAcurrent = ia;
      PhaseBcurrent = ib;
      PhaseCcurrent = ic;
      PowerT1 = t1;
      PowerT2 = t2;
    }

    public override string ToString() {
      string result = string.Empty;
      result += "���� �������:\t" + Status + Environment.NewLine;
      result += "������:\t" + (IsInFault ? "����" : "���") + Environment.NewLine;
      result += "��������� ����������:\t" + (IsTurnedOn ? "���" : "����") + Environment.NewLine;
      result += "����� �� ���������:\t" + (NoLinkWithCounter ? "������" : "�����") + Environment.NewLine;
      result += "��� ��������:\t" + CounterType + Environment.NewLine;
      result += "��������� ������� �� ��������������:\t" + (IsAutoTurnOffTimerStarted ? "�������" : "�� �������") +
                Environment.NewLine;
      result += "������ ����������� �����/��� ����� � FRAM:\t" + (NoFramOrCrcError ? "��" : "�����") +
                Environment.NewLine;
      result += "������:\t" + (NoArchives ? "�� �������" : "�������") + Environment.NewLine;

      result += "����� ��������� ������� (������):\t" + To + Environment.NewLine;
      result += "��� ���� A (�):\t" + PhaseAcurrent + Environment.NewLine;
      result += "��� ���� B (�):\t" + PhaseBcurrent + Environment.NewLine;
      result += "��� ���� C (�):\t" + PhaseCcurrent + Environment.NewLine;
      result += "������������ ������� �1 (���/�):\t" + PowerT1.ToString("f2") + Environment.NewLine;
      result += "������������ ������� �2 (���/�):\t" + PowerT2.ToString("f2") + Environment.NewLine;
      return result;
    }

    public bool IsInFault => (Status & 0x01) != 0;

    public bool IsTurnedOn => (Status & 0x02) != 0;

    public bool NoLinkWithCounter => (Status & 0x04) != 0;

    public string CounterType => (Status & 0x08) != 0 ? "�� 302" : "�� 102";

    public bool IsAutoTurnOffTimerStarted => (Status & 0x10) != 0;

    public bool NoFramOrCrcError => (Status & 0x20) != 0;

    public bool NoArchives => (Status & 0x40) != 0;
  }
}