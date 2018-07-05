using Commands.Contracts;

namespace Commands.Bumiz.CounterSe102 {
  public class ConfTarProgCommand : ICounterCommand {
    public bool UseExternalTp { get; set; }

    public ushort Code => 0x0105;

    /// <summary>
    /// Конструктор
    /// </summary>
    /// <param name="useExternalTp">Флаг блокировки</param>
    public ConfTarProgCommand(bool useExternalTp) {
      UseExternalTp = useExternalTp;
    }

    public string Comment => "Установка типа тарификации";


    public byte[] Serialize() {
      return new[] {(byte) (UseExternalTp ? 1 : 0)};
    }


    public int AwaitedBytesCount => 0;
  }
}