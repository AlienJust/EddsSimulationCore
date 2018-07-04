namespace Commands.Bumiz.CounterSe102 {
	public enum TariffProgramType : byte {
		/// <summary>
		/// Рабочий день
		/// </summary>
		Workday = 0x01,

		/// <summary>
		/// Суббота
		/// </summary>
		Saturday = 0x02,

		/// <summary>
		/// Воскресение
		/// </summary>
		Sunday = 0x03,

		/// <summary>
		/// Особый день
		/// </summary>
		SpecialDay = 0x00
	}
}