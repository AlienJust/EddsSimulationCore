using System;
using System.Collections.Generic;

namespace Controllers.Contracts {
  public interface IController {
    string Name { get; }
    void GetDataInCallback(int command, IReadOnlyList<byte> data, Action<Exception, IReadOnlyList<byte>> callback);
  }
}