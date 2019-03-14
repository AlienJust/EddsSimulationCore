using System.Collections.Generic;
using PollSystem.CommandManagement.Channels;

namespace Controllers.Lora
{
    internal sealed class InteleconAnyCommand : IInteleconCommand
    {
        public InteleconAnyCommand(string identifier, int code, IReadOnlyList<byte> data)
        {
            Identifier = identifier;
            Code = code;
            Data = data;
        }

        public string Identifier { get; }
        public int Code { get; }
        public IReadOnlyList<byte> Data { get; }
    }
}