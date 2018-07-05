using Commands.Contracts;

namespace Commands.Bumiz.Intelecon {
  public class PollCommand : IInteleconCommand {
    public byte Code => 0x01;

    public string Comment => "Чтение текущих данных";

    public byte[] Serialize() {
      return new byte[0];
    }

    public PollCommandResult GetResult(byte[] reply) {
      return new PollCommandResult
      (
        reply[0],
        reply[9],
        reply[10],
        reply[11],
        reply[12],
        0.01 * ((reply[4] << 24) + (reply[3] << 16) + (reply[2] << 8) + reply[1]),
        0.01 * ((reply[8] << 24) + (reply[7] << 16) + (reply[6] << 8) + reply[5])
      );
    }
  }
}