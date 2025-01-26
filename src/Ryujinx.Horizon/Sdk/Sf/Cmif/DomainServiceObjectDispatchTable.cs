using Ryujinx.Horizon.Common;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.Horizon.Sdk.Sf.Cmif
{
    class DomainServiceObjectDispatchTable : ServiceDispatchTableBase
    {
        public override Result ProcessMessage(ref ServiceDispatchContext context, ReadOnlySpan<byte> inRawData)
        {
            return ProcessMessageImpl(ref context, ((DomainServiceObject)context.ServiceObject).GetServerDomain(), inRawData);
        }

        private static Result ProcessMessageImpl(ref ServiceDispatchContext context, ServerDomainBase domain, ReadOnlySpan<byte> inRawData)
        {
            if (inRawData.Length < Unsafe.SizeOf<CmifDomainInHeader>())
            {
                return SfResult.InvalidHeaderSize;
            }

            CmifDomainInHeader inHeader = MemoryMarshal.Cast<byte, CmifDomainInHeader>(inRawData)[0];

            ReadOnlySpan<byte> inDomainRawData = inRawData[Unsafe.SizeOf<CmifDomainInHeader>()..];

            int targetObjectId = inHeader.ObjectId;

            switch (inHeader.Type)
            {
                case CmifDomainRequestType.SendMessage:
                    ServiceObjectHolder targetObject = domain.GetObject(targetObjectId);
                    if (targetObject == null)
                    {
                        return SfResult.TargetNotFound;
                    }

                    if (inHeader.DataSize + inHeader.ObjectsCount * sizeof(int) > inDomainRawData.Length)
                    {
                        return SfResult.InvalidHeaderSize;
                    }

                    ReadOnlySpan<byte> inMessageRawData = inDomainRawData[..inHeader.DataSize];

                    if (inHeader.ObjectsCount > DomainServiceObjectProcessor.MaximumObjects)
                    {
                        return SfResult.InvalidInObjectsCount;
                    }

                    int[] inObjectIds = new int[inHeader.ObjectsCount];

                    DomainServiceObjectProcessor domainProcessor = new(domain, inObjectIds);

                    if (context.Processor == null)
                    {
                        context.Processor = domainProcessor;
                    }
                    else
                    {
                        context.Processor.SetImplementationProcessor(domainProcessor);
                    }

                    context.ServiceObject = targetObject.ServiceObject;

                    return targetObject.ProcessMessage(ref context, inMessageRawData);

                case CmifDomainRequestType.Close:
                    domain.UnregisterObject(targetObjectId);
                    return Result.Success;

                default:
                    return SfResult.InvalidInHeader;
            }
        }
    }
}
