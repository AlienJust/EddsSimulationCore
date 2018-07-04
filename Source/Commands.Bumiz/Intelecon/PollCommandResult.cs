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
			result += "Байт статуса:\t" + Status + Environment.NewLine;
			result += "Аварии:\t" + (IsInFault ? "есть" : "нет") + Environment.NewLine;
			result += "Состояние контактора:\t" + (IsTurnedOn ? "вкл" : "откл") + Environment.NewLine;
			result += "Связь со счётчиком:\t" + (NoLinkWithCounter ? "ошибка" : "норма") + Environment.NewLine;
			result += "Тип счётчика:\t" + CounterType + Environment.NewLine;
			result += "Состояние таймера на автовыключение:\t" + (IsAutoTurnOffTimerStarted ? "запущен" : "не запущен") + Environment.NewLine;
			result += "Ошибка контрольной суммы/нет связи с FRAM:\t" + (NoFramOrCrcError ? "да" : "норма") + Environment.NewLine;
			result += "Архивы:\t" + (NoArchives ? "не ведутся" : "ведутся") + Environment.NewLine;

			result += "Время защитного таймера (минуты):\t" + To + Environment.NewLine;
			result += "Ток фазы A (А):\t" + PhaseAcurrent + Environment.NewLine;
			result += "Ток фазы B (А):\t" + PhaseBcurrent + Environment.NewLine;
			result += "Ток фазы C (А):\t" + PhaseCcurrent + Environment.NewLine;
			result += "Потребленная энергия Т1 (кВт/ч):\t" + PowerT1.ToString("f2") + Environment.NewLine;
			result += "Потребленная энергия Т2 (кВт/ч):\t" + PowerT2.ToString("f2") + Environment.NewLine;
			return result;
		}

		public bool IsInFault => (Status & 0x01) != 0;

		public bool IsTurnedOn => (Status & 0x02) != 0;

		public bool NoLinkWithCounter => (Status & 0x04) != 0;

		public string CounterType => (Status & 0x08) != 0 ? "СЕ 302" : "СЕ 102";

		public bool IsAutoTurnOffTimerStarted => (Status & 0x10) != 0;

		public bool NoFramOrCrcError => (Status & 0x20) != 0;

		public bool NoArchives => (Status & 0x40) != 0;
	}
}