using Commands.Contracts;

namespace Commands.Bumiz.CounterSe102 {
  public class ConfSpecDaysCommand : ICounterCommand {
    public bool EnableSpecDaysTp { get; set; }

    public ushort Code => 0x0107;

    /// <summary>
    /// Конструктор
    /// </summary>
    /// <param name="enableSpecDaysTp">Флаг разрешения тарифной программы</param>
    public ConfSpecDaysCommand(bool enableSpecDaysTp) {
      EnableSpecDaysTp = enableSpecDaysTp;
    }

    public string Comment => "Конфигурация тарификации выходных и праздничных дней";


    public byte[] Serialize() {
      return new[] {((byte) (EnableSpecDaysTp ? 1 : 0))};
    }


    public int AwaitedBytesCount => 0;
  }
}