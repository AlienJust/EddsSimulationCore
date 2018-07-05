using Commands.Contracts;

namespace Commands.Bumiz.CounterSe102 {
  public class SetDayLightSavingTimeCommand : ICounterCommand {
    private readonly bool _enableDaylightSaving;

    public SetDayLightSavingTimeCommand(bool enableDaylightSaving) {
      _enableDaylightSaving = enableDaylightSaving;
    }

    public ushort Code => 0x0106;

    public string Comment => "Установка перехода на летнее время";

    public byte[] Serialize() {
      var query = new byte[] {0x00};
      if (_enableDaylightSaving) query[0] = 0x01;

      return query; //.GetCounterQuery(Code);
    }

    public int AwaitedBytesCount => 0;
  }
}