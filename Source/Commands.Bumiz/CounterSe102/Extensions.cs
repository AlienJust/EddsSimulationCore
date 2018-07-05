using System;

namespace Commands.Bumiz.CounterSe102 {
  public static class Extensions {
    /// <summary>
    /// Оборачивает данные запроса к счётчику в формат Ромы Тарасова (часть формата СЕ102)
    /// </summary>
    /// <param name="queryToCounter">Байты запроса</param>
    /// <param name="commandCode">Код команды счётчика</param>
    /// <returns>Результирующие байты запроса</returns>
    public static byte[] GetCounterQuery(this byte[] queryToCounter, ushort commandCode) {
      var result = new byte[4 + queryToCounter.Length];
      result[0] = 0x04;
      result[1] = (byte) (0xD0 | queryToCounter.Length);
      result[2] = (byte) ((commandCode & 0xFF00) >> 8);
      result[3] = (byte) (commandCode & 0x00FF);
      queryToCounter.CopyTo(result, 4);
      return result;
    }

    /// <summary>
    /// Проводит базовую проверку правильности ответа счётчика
    /// </summary>
    /// <param name="counterReply">Ответ счётчика</param>
    /// <param name="commandCode">Код команды</param>
    /// <returns>Признак правильности ответа</returns>
    public static bool IsCounterReplyCorrect(this byte[] counterReply, ushort commandCode) {
      if ((counterReply[0] & 0x70) == 0x70) {
        return false;
      }

      if ((counterReply[0] & 0x50) == 0x50) {
        if ((counterReply[0] & 0x0F) == counterReply.Length - 3) {
          if ((byte) ((commandCode & 0xFF00) >> 8) == counterReply[1] &&
              (byte) (commandCode & 0x00FF) == counterReply[2]) {
            return true;
          }
        }
      }

      return false; // default is false
    }

    public static byte[] GetDataBytesFromCounterReply(this byte[] reply) {
      var result = new byte[reply.Length - 3];
      for (int i = 0; i < result.Length; ++i)
        result[i] = reply[i + 3];

      return result;
    }

    public static TimeSpan HalfsAnHourToTimeSpan(this int hl) {
      // For example, if hl=15 then: 15/2=7 and 15%2 is not 0  =>  7:30 will be returned
      return new TimeSpan(hl / 2, hl % 2 == 0 ? 0 : 30, 0);
    }
  }
}