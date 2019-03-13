using System.Collections.Generic;
using PollSystem.CommandManagement.Channels;

namespace Controllers.Lora
{
    internal sealed class InteleconAnyCommand : IInteleconCommand
    {
        public InteleconAnyCommand(object identifier, int code, IReadOnlyList<byte> data)
        {
            Identifier = identifier;
            Code = code;
            Data = data;
        }

        public object Identifier { get; }
        public int Code { get; }
        public IReadOnlyList<byte> Data { get; }
    }
}