using System;

namespace Commands.Bumiz.Intelecon {
	public struct ReadSettingsCommandResult {
		public ReadSettingsCommandResult(ushort inteleconAddr, byte controlWord, byte thresholdCurrent, byte thresholdTime, byte protectionTimeout, byte autoPowerOnTimeout)
			: this()
		{
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

		public override string ToString()
		{
			
			string result = string.Empty;
			result += "Адрес Интелекон: \t" + InteleconAddr + Environment.NewLine;
			result += "Контрольное слово: \t" + ControlWord + Environment.NewLine;
			result += "Порог отключения по току: \t" + ThresholdCurrent + Environment.NewLine;
			result += "Время отключения: \t" + ThresholdTime + Environment.NewLine;
			result += "Таймаут защиты: \t" + ProtectionTimeout + Environment.NewLine;
			result += "Таймаут автоматического отключения: \t" + AutoPowerOnTimeout + Environment.NewLine;
			return result;
		}
	}
}