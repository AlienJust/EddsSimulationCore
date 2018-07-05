using System;
using System.Globalization;
using Audience;
using Commands.Contracts;

namespace Commands.Bumiz.CounterSe102 {
  public class SetCounterTimeToCurrentCommand : ICounterCommand {
    private readonly TimeSpan _offset;

    /// <summary>
    /// Инициализирует новый экземпляр класса
    /// </summary>
    /// <param name="offset">Смещение которое будет сложено с текущем временем во время вызова метода Serialize()</param>
    public SetCounterTimeToCurrentCommand(TimeSpan offset) {
      _offset = offset;
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса
    /// </summary>
    public SetCounterTimeToCurrentCommand() {
      _offset = new TimeSpan(0);
    }

    public ushort Code => 0x0121;


    public string Comment => "Запись текущего времени в качестве времени счётчика";


    public byte[] Serialize() {
      // Важное отличие от SetCounterTimeCommand в том, что записываемое время определяется в момент синхронизации
      var timeToSet = DateTime.Now.Add(_offset);
      var query = new byte[7];
      query[6] = (byte) int.Parse((timeToSet.Year - 2000).ToString(CultureInfo.InvariantCulture),
        NumberStyles.HexNumber);
      query[5] = (byte) int.Parse(timeToSet.Month.ToString(CultureInfo.InvariantCulture), NumberStyles.HexNumber);
      query[4] = (byte) int.Parse(timeToSet.Day.ToString(CultureInfo.InvariantCulture), NumberStyles.HexNumber);
      query[3] = (byte) timeToSet.GetDayOfWeekNumber();
      query[2] = (byte) int.Parse(timeToSet.Hour.ToString(CultureInfo.InvariantCulture), NumberStyles.HexNumber);
      query[1] = (byte) int.Parse(timeToSet.Minute.ToString(CultureInfo.InvariantCulture), NumberStyles.HexNumber);
      query[0] = (byte) int.Parse(timeToSet.Second.ToString(CultureInfo.InvariantCulture), NumberStyles.HexNumber);
      return query;
    }


    public int AwaitedBytesCount => 0;
  }
}