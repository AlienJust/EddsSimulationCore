namespace Controllers.Bumiz {
  interface IBumizControllerInfo {
    string Name { get; }
    int CurrentDataCacheTtlSeconds { get; }
    string Pulse1Expression { get; }
    string Pulse2Expression { get; }
    string Pulse3Expression { get; }
  }
}