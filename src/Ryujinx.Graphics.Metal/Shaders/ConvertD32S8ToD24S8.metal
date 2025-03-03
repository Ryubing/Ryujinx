#include <metal_stdlib>

using namespace metal;

struct StrideArguments {
    int pixelCount;
    int dstStartOffset;
};

struct InData {
    uint data[1];
};

struct OutData {
    uint data[1];
};

struct ConstantBuffers {
    constant StrideArguments* stride_arguments;
};

struct StorageBuffers {
    device InData* in_data;
    device OutData* out_data;
};

kernel void kernelMain(constant ConstantBuffers &constant_buffers [[buffer(CONSTANT_BUFFERS_INDEX)]],
                       device StorageBuffers &storage_buffers [[buffer(STORAGE_BUFFERS_INDEX)]],
                       uint3 thread_position_in_grid [[thread_position_in_grid]],
                       uint3 threads_per_threadgroup [[threads_per_threadgroup]],
                       uint3 threadgroups_per_grid [[threadgroups_per_grid]])
{
    // Determine what slice of the stride copies this invocation will perform.
    int invocations = int(threads_per_threadgroup.x * threadgroups_per_grid.x);

    int copiesRequired = constant_buffers.stride_arguments->pixelCount;

    // Find the copies that this invocation should perform.

    // - Copies that all invocations perform.
    int allInvocationCopies = copiesRequired / invocations;

    // - Extra remainder copy that this invocation performs.
    int index = int(thread_position_in_grid.x);
    int extra = (index < (copiesRequired % invocations)) ? 1 : 0;

    int copyCount = allInvocationCopies + extra;

    // Finally, get the starting offset. Make sure to count extra copies.

    int startCopy = allInvocationCopies * index + min(copiesRequired % invocations, index);

    int srcOffset = startCopy * 2;
    int dstOffset = constant_buffers.stride_arguments->dstStartOffset + startCopy;

    // Perform the conversion for this region.
    for (int i = 0; i < copyCount; i++)
    {
        float depth = as_type<float>(storage_buffers.in_data->data[srcOffset++]);
        uint stencil = storage_buffers.in_data->data[srcOffset++];

        uint rescaledDepth = uint(clamp(depth, 0.0, 1.0) * 16777215.0);

        storage_buffers.out_data->data[dstOffset++] = (rescaledDepth << 8) | (stencil & 0xff);
    }
}
