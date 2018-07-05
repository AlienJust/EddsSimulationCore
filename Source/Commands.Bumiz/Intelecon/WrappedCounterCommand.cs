using Commands.Bumiz.CounterSe102;
using Commands.Contracts;

namespace Commands.Bumiz.Intelecon {
  public class WrappedCounterCommand : IInteleconCommand {
    public byte Code => 0x09;

    public string Comment => CounterCommand.Comment;

    public ICounterCommand CounterCommand { get; }

    public WrappedCounterCommand(ICounterCommand commandToWrap) {
      CounterCommand = commandToWrap;
    }

    public byte[] Serialize() {
      return CounterCommand.Serialize().GetCounterQuery(CounterCommand.Code);
    }
  }
}