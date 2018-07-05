using Commands.Contracts;

namespace Commands.Bumiz.Intelecon {
  public class ReadSettingsCommand : IInteleconCommand {
    public byte Code => 0x03;

    public string Comment => "Чтение настроек прибора БУМИЗ";

    public byte[] Serialize() {
      return new byte[] {1};
    }


    public ReadSettingsCommandResult GetResult(byte[] inBytes, ushort addressInReply) {
      var inteleconAddr = addressInReply;
      var controlWord = inBytes[1];
      var thresholdCurrent = inBytes[2];
      var thresholdTime = inBytes[3];
      var protectionTimeout = inBytes[4];
      var autoPowerOnTimeout = inBytes[5];
      return new ReadSettingsCommandResult(inteleconAddr, controlWord, thresholdCurrent, thresholdTime,
        protectionTimeout, autoPowerOnTimeout);
    }
  }
}