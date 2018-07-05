using System.Collections.Generic;
using Commands.Contracts;

namespace Commands.Bumiz.Counters.Mnemonic {
  public class SetSettingsLockCommand : ICounterCommand {
    private readonly bool _isLocked;

    public SetSettingsLockCommand(bool isLocked) {
      _isLocked = isLocked;
    }

    public ushort Code => 0x0FFF;

    public string Comment => _isLocked ? "Б" : "Разб" + "локировка настроек";


    public byte[] Serialize() {
      return new[] {_isLocked ? (byte) 0 : (byte) 1};
    }


    public int AwaitedBytesCount => 1;

    public bool IsCommandSucceed(IList<byte> reply) {
      if (reply[0] == 0x06) return true;
      return false;
    }
  }
}