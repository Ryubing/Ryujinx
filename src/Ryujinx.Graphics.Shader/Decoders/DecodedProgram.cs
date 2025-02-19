using Ryujinx.Graphics.Shader.Translation;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Shader.Decoders
{
    readonly struct DecodedProgram : IEnumerable<DecodedFunction>
    {
        public DecodedFunction MainFunction { get; }
        private readonly IReadOnlyDictionary<ulong, DecodedFunction> _functions;
        private readonly List<DecodedFunction> _functionsWithId;
        public int FunctionsWithIdCount => _functionsWithId.Count;

        public AttributeUsage AttributeUsage { get; }
        public FeatureFlags UsedFeatures { get; }
        public byte ClipDistancesWritten { get; }
        public int Cb1DataSize { get; }

        public DecodedProgram(
            DecodedFunction mainFunction,
            IReadOnlyDictionary<ulong, DecodedFunction> functions,
            AttributeUsage attributeUsage,
            FeatureFlags usedFeatures,
            byte clipDistancesWritten,
            int cb1DataSize)
        {
            MainFunction = mainFunction;
            _functions = functions;
            _functionsWithId = [];
            AttributeUsage = attributeUsage;
            UsedFeatures = usedFeatures;
            ClipDistancesWritten = clipDistancesWritten;
            Cb1DataSize = cb1DataSize;
        }

        public DecodedFunction GetFunctionByAddress(ulong address)
        {
            if (_functions.TryGetValue(address, out DecodedFunction function))
            {
                return function;
            }

            return null;
        }

        public DecodedFunction GetFunctionById(int id)
        {
            if ((uint)id >= (uint)_functionsWithId.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            return _functionsWithId[id];
        }

        public void AddFunctionAndSetId(DecodedFunction function)
        {
            function.Id = _functionsWithId.Count;
            _functionsWithId.Add(function);
        }

        public IoUsage GetIoUsage()
        {
            return new IoUsage(UsedFeatures, ClipDistancesWritten, AttributeUsage.UsedOutputAttributes);
        }

        public IEnumerator<DecodedFunction> GetEnumerator()
        {
            return _functions.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
