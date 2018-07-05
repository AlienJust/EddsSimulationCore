namespace Commands.Bumiz.CounterSe102 {
  public enum TariffProgramType : byte {
    /// <summary>
    /// ������� ����
    /// </summary>
    Workday = 0x01,

    /// <summary>
    /// �������
    /// </summary>
    Saturday = 0x02,

    /// <summary>
    /// �����������
    /// </summary>
    Sunday = 0x03,

    /// <summary>
    /// ������ ����
    /// </summary>
    SpecialDay = 0x00
  }
}