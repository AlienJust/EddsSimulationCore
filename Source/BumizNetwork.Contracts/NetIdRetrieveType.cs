namespace BumizNetwork.Contracts {
	/// <summary>
	/// Тип адресации объектов
	/// </summary>
	public enum NetIdRetrieveType {
		/// <summary>
		/// Адресация по сетевому адресу Интелекон
		/// </summary>
		InteleconAddress,

		/// <summary>
		/// Адресация по серийному номеру устройства
		/// </summary>
		SerialNumber,

		/// <summary>
		/// Адресация по сетевому адресу Интелекон (для старой прошивки)
		/// </summary>
		OldProtocolSerialNumber
	}
}