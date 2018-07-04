using System;

namespace Commands.Bumiz.CounterSe102 {
	public static class TariffProgramExtensions {
		public static string GetDescription(this TariffProgramType tp) {
			switch (tp) {
				case TariffProgramType.Workday:
					return "Рабочий день";
				case TariffProgramType.SpecialDay:
					return "Особый день";
				case TariffProgramType.Saturday:
					return "Субботний день";
				case TariffProgramType.Sunday:
					return "Воскресный день";

			}
			throw new Exception("Cannot get description");
		}
	}
}