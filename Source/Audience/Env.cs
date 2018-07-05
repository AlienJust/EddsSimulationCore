using System;
using System.IO;
using AJ.Std.Loggers;
using AJ.Std.Loggers.Contracts;

namespace Audience {
  public class Env {
    public static string CfgPath => "cfg";
    public static string LogPath => "log";

    public static ILogger GlobalLog { get; } = new FileLogger(Path.Combine(LogPath,
      "GlobalSharedLog_" + DateTime.Now.ToString("yyyy.MM.dd_HH.mm.ss") + ".txt"));
  }
}