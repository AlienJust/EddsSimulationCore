using System;
using System.Collections.Generic;
using System.Linq;
using Controllers.Contracts;

namespace Controllers.Gateway {
  class GateController : IController {
    private readonly Random _random = new Random();
    public string Name { get; }

    public GateController(string name) {
      Name = name;
    }

    public void GetDataInCallback(int command, IEnumerable<byte> data, Action<Exception, IEnumerable<byte>> callback) {
      try {
        switch (command) {
          case 1:
            var result1 = new byte[1];
            _random.NextBytes(result1);
            callback(null, result1);
            break;
          case 6:
            var result6 = data.ToList();
            // Special type of attached counter = 250 means that command sended to gateway controller itself (c) Danila
            if (result6[1] == 250) {
              result6.Add((byte) _random.Next(256));
              result6.AddRange(BitConverter.GetBytes(Environment.TickCount / 60000));
              callback(null, result6);
              break;
            }

            throw new Exception("Attached command is skipped by gateway controller because it attached counter type is not equals 250");
          default:
            throw new Exception("Intelecon command with code " + command + " is not supported by gateway controller");
        }
      }
      catch (Exception ex) {
        callback(ex, null);
      }
    }
  }
}