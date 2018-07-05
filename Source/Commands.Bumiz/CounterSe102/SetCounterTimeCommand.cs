using System;
using System.Globalization;
using Audience;
using Commands.Contracts;

namespace Commands.Bumiz.CounterSe102 {
  public class SetCounterTimeCommand : ICounterCommand {
    private DateTime _timeToSet;


    public ushort Code => 0x0121;


    public string Comment => "Запись времени счётчика";

    public SetCounterTimeCommand(DateTime timeToSet) {
      _timeToSet = timeToSet;
    }


    public byte[] Serialize() {
      var query = new byte[7];
      query[6] = (byte) int.Parse((_timeToSet.Year - 2000).ToString(CultureInfo.InvariantCulture),
        NumberStyles.HexNumber);
      query[5] = (byte) int.Parse(_timeToSet.Month.ToString(CultureInfo.InvariantCulture), NumberStyles.HexNumber);
      query[4] = (byte) int.Parse(_timeToSet.Day.ToString(CultureInfo.InvariantCulture), NumberStyles.HexNumber);
      query[3] = (byte) _timeToSet.GetDayOfWeekNumber();
      query[2] = (byte) int.Parse(_timeToSet.Hour.ToString(CultureInfo.InvariantCulture), NumberStyles.HexNumber);
      query[1] = (byte) int.Parse(_timeToSet.Minute.ToString(CultureInfo.InvariantCulture), NumberStyles.HexNumber);
      query[0] = (byte) int.Parse(_timeToSet.Second.ToString(CultureInfo.InvariantCulture), NumberStyles.HexNumber);
      //return (query).GetCounterQuery(Code);
      return query;
    }


    public int AwaitedBytesCount => 0;
  }
}