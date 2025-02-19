using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Shader.Decoders
{
    class DecodedFunction
    {
        private readonly HashSet<DecodedFunction> _callers;

        public bool IsCompilerGenerated => Type != FunctionType.User;
        public FunctionType Type { get; set; }
        public int Id { get; set; }

        public ulong Address { get; }
        public Block[] Blocks { get; private set; }

        public DecodedFunction(ulong address)
        {
            Address = address;
            _callers = [];
            Type = FunctionType.User;
            Id = -1;
        }

        public void SetBlocks(Block[] blocks)
        {
            if (Blocks != null)
            {
                throw new InvalidOperationException("Blocks have already been set.");
            }

            Blocks = blocks;
        }

        public void AddCaller(DecodedFunction caller)
        {
            _callers.Add(caller);
        }

        public void RemoveCaller(DecodedFunction caller)
        {
            if (_callers.Remove(caller) && _callers.Count == 0)
            {
                Type = FunctionType.Unused;
            }
        }
    }
}
