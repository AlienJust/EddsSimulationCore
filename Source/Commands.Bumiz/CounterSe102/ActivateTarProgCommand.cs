using Commands.Contracts;

namespace Commands.Bumiz.CounterSe102 {
  public class ActivateTarProgCommand : ICounterCommand {
    public ushort Code => 0x013F;


    public string Comment => "Активация тарифной программы";


    public byte[] Serialize() {
      return new byte[0];
    }


    public int AwaitedBytesCount => 1;
  }
}