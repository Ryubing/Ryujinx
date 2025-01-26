using Ryujinx.Common.Memory;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Ns;
using Ryujinx.Horizon.Sdk.Sf.Cmif;
using Ryujinx.Horizon.Sdk.Sf.Hipc;
using Ryujinx.Horizon.Sdk.Sm;
using System;
using System.Runtime.CompilerServices;

namespace Ryujinx.Horizon.Sdk.Arp
{
    class ArpApi : IDisposable
    {
        private const string ArpRName = "arp:r";

        private readonly HeapAllocator _allocator;
        private int _sessionHandle;

        public ArpApi(HeapAllocator allocator)
        {
            _allocator = allocator;
        }

        private void InitializeArpRService()
        {
            if (_sessionHandle == 0)
            {
                using SmApi smApi = new();

                smApi.Initialize();
                smApi.GetServiceHandle(out _sessionHandle, ServiceName.Encode(ArpRName)).AbortOnFailure();
            }
        }

        public Result GetApplicationInstanceId(out ulong applicationInstanceId, ulong applicationPid)
        {
            Span<byte> data = stackalloc byte[8];
            SpanWriter writer = new(data);

            writer.Write(applicationPid);

            InitializeArpRService();

            Result result = ServiceUtil.SendRequest(out CmifResponse response, _sessionHandle, 3, sendPid: false, data);
            if (result.IsFailure)
            {
                applicationInstanceId = 0;

                return result;
            }

            SpanReader reader = new(response.Data);

            applicationInstanceId = reader.Read<ulong>();

            return Result.Success;
        }

        public Result GetApplicationLaunchProperty(out ApplicationLaunchProperty applicationLaunchProperty, ulong applicationInstanceId)
        {
            applicationLaunchProperty = default;

            Span<byte> data = stackalloc byte[8];
            SpanWriter writer = new(data);

            writer.Write(applicationInstanceId);

            InitializeArpRService();

            Result result = ServiceUtil.SendRequest(out CmifResponse response, _sessionHandle, 0, sendPid: false, data);
            if (result.IsFailure)
            {
                return result;
            }

            SpanReader reader = new(response.Data);

            applicationLaunchProperty = reader.Read<ApplicationLaunchProperty>();

            return Result.Success;
        }

        public Result GetApplicationControlProperty(out ApplicationControlProperty applicationControlProperty, ulong applicationInstanceId)
        {
            applicationControlProperty = default;

            Span<byte> data = stackalloc byte[8];
            SpanWriter writer = new(data);

            writer.Write(applicationInstanceId);

            ulong bufferSize = (ulong)Unsafe.SizeOf<ApplicationControlProperty>();
            ulong bufferAddress = _allocator.Allocate(bufferSize);

            InitializeArpRService();

            Result result = ServiceUtil.SendRequest(
                out CmifResponse response,
                _sessionHandle,
                1,
                sendPid: false,
                data,
                [HipcBufferFlags.Out | HipcBufferFlags.MapAlias | HipcBufferFlags.FixedSize],
                [new PointerAndSize(bufferAddress, bufferSize)]);

            if (result.IsFailure)
            {
                return result;
            }

            applicationControlProperty = HorizonStatic.AddressSpace.Read<ApplicationControlProperty>(bufferAddress);

            _allocator.Free(bufferAddress, bufferSize);

            return Result.Success;
        }

        public void Dispose()
        {
            if (_sessionHandle != 0)
            {
                HorizonStatic.Syscall.CloseHandle(_sessionHandle);

                _sessionHandle = 0;
            }

            GC.SuppressFinalize(this);
        }
    }
}
