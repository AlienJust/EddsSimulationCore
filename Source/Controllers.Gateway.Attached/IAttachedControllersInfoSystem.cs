namespace Controllers.Gateway.Attached {
  public interface IAttachedControllersInfoSystem {
    string GetAttachedControllerNameByConfig(string gateway, int channel, int type, int number);
  }
}