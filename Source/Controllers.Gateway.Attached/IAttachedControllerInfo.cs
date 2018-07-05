namespace Controllers.Gateway.Attached {
  public interface IAttachedControllerInfo {
    string Gateway { get; }
    int Channel { get; }
    int Type { get; }
    int Number { get; }
    string Name { get; }
  }
}