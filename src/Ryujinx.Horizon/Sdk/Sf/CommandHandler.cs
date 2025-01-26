using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf.Cmif;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.Horizon.Sdk.Sf
{
    class CommandHandler
    {
        public delegate Result MethodInvoke(
            ref ServiceDispatchContext context,
            HipcCommandProcessor processor,
            ServerMessageRuntimeMetadata runtimeMetadata,
            ReadOnlySpan<byte> inRawData,
            ref Span<CmifOutHeader> outHeader);

        private readonly MethodInvoke _invoke;
        private readonly HipcCommandProcessor _processor;

        public string MethodName => _invoke.Method.Name;

        public CommandHandler(MethodInvoke invoke, params CommandArg[] args)
        {
            _invoke = invoke;
            _processor = new HipcCommandProcessor(args);
        }

        public Result Invoke(ref Span<CmifOutHeader> outHeader, ref ServiceDispatchContext context, ReadOnlySpan<byte> inRawData)
        {
            if (context.Processor == null)
            {
                context.Processor = _processor;
            }
            else
            {
                context.Processor.SetImplementationProcessor(_processor);
            }

            ServerMessageRuntimeMetadata runtimeMetadata = context.Processor.GetRuntimeMetadata();
            Result result = context.Processor.PrepareForProcess(ref context, runtimeMetadata);

            return result.IsFailure ? result : _invoke(ref context, _processor, runtimeMetadata, inRawData, ref outHeader);
        }

        public static void GetCmifOutHeaderPointer(ref Span<CmifOutHeader> outHeader, ref Span<byte> outRawData)
        {
            outHeader = MemoryMarshal.Cast<byte, CmifOutHeader>(outRawData)[..1];
            outRawData = outRawData[Unsafe.SizeOf<CmifOutHeader>()..];
        }
    }
}
