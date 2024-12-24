#include <metal_stdlib>

using namespace metal;

struct IndexBufferPattern {
    int pattern[8];
    int primitiveVertices;
    int primitiveVerticesOut;
    int indexSize;
    int indexSizeOut;
    int baseIndex;
    int indexStride;
    int srcOffset;
    int totalPrimitives;
};

struct InData {
    uint8_t data[1];
};

struct OutData {
    uint8_t data[1];
};

struct StorageBuffers {
    device InData* in_data;
    device OutData* out_data;
    constant IndexBufferPattern* index_buffer_pattern;
};

kernel void kernelMain(device StorageBuffers &storage_buffers [[buffer(STORAGE_BUFFERS_INDEX)]],
                       uint3 thread_position_in_grid [[thread_position_in_grid]])
{
    int primitiveIndex = int(thread_position_in_grid.x);
    if (primitiveIndex >= storage_buffers.index_buffer_pattern->totalPrimitives)
    {
        return;
    }

    int inOffset = primitiveIndex * storage_buffers.index_buffer_pattern->indexStride;
    int outOffset = primitiveIndex * storage_buffers.index_buffer_pattern->primitiveVerticesOut;

    for (int i = 0; i < storage_buffers.index_buffer_pattern->primitiveVerticesOut; i++)
    {
        int j;
        int io = max(0, inOffset + storage_buffers.index_buffer_pattern->baseIndex + storage_buffers.index_buffer_pattern->pattern[i]) * storage_buffers.index_buffer_pattern->indexSize;
        int oo = (outOffset + i) * storage_buffers.index_buffer_pattern->indexSizeOut;

        for (j = 0; j < storage_buffers.index_buffer_pattern->indexSize; j++)
        {
            storage_buffers.out_data->data[oo + j] = storage_buffers.in_data->data[storage_buffers.index_buffer_pattern->srcOffset + io + j];
        }

        for(; j < storage_buffers.index_buffer_pattern->indexSizeOut; j++)
        {
            storage_buffers.out_data->data[oo + j] = uint8_t(0);
        }
    }
}
