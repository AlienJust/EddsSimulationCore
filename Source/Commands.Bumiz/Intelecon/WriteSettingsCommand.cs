using Commands.Contracts;

namespace Commands.Bumiz.Intelecon {
  public class WriteSettingsCommand : IInteleconCommand {
    public int PinCode { get; }
    public byte ControlWord { get; }
    public byte ThresholdCurrent { get; }
    public byte ThresholdTime { get; }
    public byte ProtectionTimeout { get; }
    public byte AutoPowerOnTimeout { get; }

    public WriteSettingsCommand(int pinCode, byte controlWord, byte thresholdCurrent, byte thresholdTime,
      byte protectionTimeout, byte autoPowerOnTimeout) {
      PinCode = pinCode;
      ControlWord = controlWord;
      ThresholdCurrent = thresholdCurrent;
      ThresholdTime = thresholdTime;
      ProtectionTimeout = protectionTimeout;
      AutoPowerOnTimeout = autoPowerOnTimeout;
    }

    public byte Code => 0x04;

    public string Comment => "Запись настроек";

    public byte[] Serialize() {
      var query = new byte[10];
      query[0] = 0x01;

      query[1] = (byte) (PinCode & 0x000000FF);
      query[2] = (byte) ((PinCode & 0x0000FF00) >> 8);
      query[3] = (byte) ((PinCode & 0x00FF0000) >> 16);
      query[4] = (byte) ((PinCode & 0xFF000000) >> 24);

      query[5] = ControlWord;
      query[6] = ThresholdCurrent;
      query[7] = ThresholdTime;
      query[8] = ProtectionTimeout;
      query[9] = AutoPowerOnTimeout;

      return query;
    }

    public bool GetResult(byte[] reply) {
      return reply.Length == 1 && reply[0] == 0x01;
    }
  }
}