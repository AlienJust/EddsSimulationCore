using System;

namespace Commands.Bumiz.Intelecon {
  public interface IAdvancedArchiveResult3 {
    TimeSpan Time { get; }
    int HotWater1 { get; }
    int HotWater2 { get; }

    int ColdWater1 { get; }
    int ColdWater2 { get; }

    int Gas1 { get; }
    int Gas2 { get; }

    int Xstatus { get; }
  }
}