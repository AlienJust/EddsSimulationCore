using System;

namespace Commands.Bumiz.CounterSe102 {
  public static class TariffProgramExtensions {
    public static string GetDescription(this TariffProgramType tp) {
      switch (tp) {
        case TariffProgramType.Workday:
          return "������� ����";
        case TariffProgramType.SpecialDay:
          return "������ ����";
        case TariffProgramType.Saturday:
          return "��������� ����";
        case TariffProgramType.Sunday:
          return "���������� ����";
      }

      throw new Exception("Cannot get description");
    }
  }
}