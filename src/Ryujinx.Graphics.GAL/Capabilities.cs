using Ryujinx.Graphics.Shader.Translation;

namespace Ryujinx.Graphics.GAL
{
    public readonly struct Capabilities
    {
        public readonly TargetApi Api;
        public readonly string VendorName;
        public readonly SystemMemoryType MemoryType;

        public readonly bool HasFrontFacingBug;
        public readonly bool HasVectorIndexingBug;
        public readonly bool NeedsFragmentOutputSpecialization;
        public readonly bool ReduceShaderPrecision;

        public readonly bool SupportsAstcCompression;
        public readonly bool SupportsBc123Compression;
        public readonly bool SupportsBc45Compression;
        public readonly bool SupportsBc67Compression;
        public readonly bool SupportsEtc2Compression;
        public readonly bool Supports3DTextureCompression;
        public readonly bool SupportsBgraFormat;
        public readonly bool SupportsR4G4Format;
        public readonly bool SupportsR4G4B4A4Format;
        public readonly bool SupportsScaledVertexFormats;
        public readonly bool SupportsSnormBufferTextureFormat;
        public readonly bool SupportsSparseBuffer;
        public readonly bool Supports5BitComponentFormat;
        public readonly bool SupportsBlendEquationAdvanced;
        public readonly bool SupportsFragmentShaderInterlock;
        public readonly bool SupportsFragmentShaderOrderingIntel;
        public readonly bool SupportsGeometryShader;
        public readonly bool SupportsGeometryShaderPassthrough;
        public readonly bool SupportsTransformFeedback;
        public readonly bool SupportsImageLoadFormatted;
        public readonly bool SupportsLayerVertexTessellation;
        public readonly bool SupportsMismatchingViewFormat;
        public readonly bool SupportsCubemapView;
        public readonly bool SupportsNonConstantTextureOffset;
        public readonly bool SupportsQuads;
        public readonly bool SupportsSeparateSampler;
        public readonly bool SupportsShaderBallot;
        public readonly bool SupportsShaderBarrierDivergence;
        public readonly bool SupportsShaderFloat64;
        public readonly bool SupportsTextureGatherOffsets;
        public readonly bool SupportsTextureShadowLod;
        public readonly bool SupportsVertexStoreAndAtomics;
        public readonly bool SupportsViewportIndexVertexTessellation;
        public readonly bool SupportsViewportMask;
        public readonly bool SupportsViewportSwizzle;
        public readonly bool SupportsIndirectParameters;
        public readonly bool SupportsDepthClipControl;
        public readonly bool SupportsExtendedDynamicState;
        public readonly bool SupportsExtendedDynamicState2;
        public readonly bool SupportsLogicOpDynamicState;
        public readonly bool SupportsPatchControlPointsDynamicState;

        public readonly int UniformBufferSetIndex;
        public readonly int StorageBufferSetIndex;
        public readonly int TextureSetIndex;
        public readonly int ImageSetIndex;
        public readonly int ExtraSetBaseIndex;
        public readonly int MaximumExtraSets;

        public readonly uint MaximumUniformBuffersPerStage;
        public readonly uint MaximumStorageBuffersPerStage;
        public readonly uint MaximumTexturesPerStage;
        public readonly uint MaximumImagesPerStage;

        public readonly int MaximumComputeSharedMemorySize;
        public readonly float MaximumSupportedAnisotropy;
        public readonly int ShaderSubgroupSize;
        public readonly int StorageBufferOffsetAlignment;
        public readonly int TextureBufferOffsetAlignment;

        public readonly int GatherBiasPrecision;

        public readonly ulong MaximumGpuMemory;

        public Capabilities(
            TargetApi api,
            string vendorName,
            SystemMemoryType memoryType,
            bool hasFrontFacingBug,
            bool hasVectorIndexingBug,
            bool needsFragmentOutputSpecialization,
            bool reduceShaderPrecision,
            bool supportsAstcCompression,
            bool supportsBc123Compression,
            bool supportsBc45Compression,
            bool supportsBc67Compression,
            bool supportsEtc2Compression,
            bool supports3DTextureCompression,
            bool supportsBgraFormat,
            bool supportsR4G4Format,
            bool supportsR4G4B4A4Format,
            bool supportsScaledVertexFormats,
            bool supportsSnormBufferTextureFormat,
            bool supports5BitComponentFormat,
            bool supportsSparseBuffer,
            bool supportsBlendEquationAdvanced,
            bool supportsFragmentShaderInterlock,
            bool supportsFragmentShaderOrderingIntel,
            bool supportsGeometryShader,
            bool supportsGeometryShaderPassthrough,
            bool supportsTransformFeedback,
            bool supportsImageLoadFormatted,
            bool supportsLayerVertexTessellation,
            bool supportsMismatchingViewFormat,
            bool supportsCubemapView,
            bool supportsNonConstantTextureOffset,
            bool supportsQuads,
            bool supportsSeparateSampler,
            bool supportsShaderBallot,
            bool supportsShaderBarrierDivergence,
            bool supportsShaderFloat64,
            bool supportsTextureGatherOffsets,
            bool supportsTextureShadowLod,
            bool supportsVertexStoreAndAtomics,
            bool supportsViewportIndexVertexTessellation,
            bool supportsViewportMask,
            bool supportsViewportSwizzle,
            bool supportsIndirectParameters,
            bool supportsDepthClipControl,
            bool supportsExtendedDynamicState,
            bool supportsExtendedDynamicState2,
            bool supportsLogicOpDynamicState,
            bool supportsPatchControlPointsDynamicState,
            int uniformBufferSetIndex,
            int storageBufferSetIndex,
            int textureSetIndex,
            int imageSetIndex,
            int extraSetBaseIndex,
            int maximumExtraSets,
            uint maximumUniformBuffersPerStage,
            uint maximumStorageBuffersPerStage,
            uint maximumTexturesPerStage,
            uint maximumImagesPerStage,
            int maximumComputeSharedMemorySize,
            float maximumSupportedAnisotropy,
            int shaderSubgroupSize,
            int storageBufferOffsetAlignment,
            int textureBufferOffsetAlignment,
            int gatherBiasPrecision,
            ulong maximumGpuMemory)
        {
            Api = api;
            VendorName = vendorName;
            MemoryType = memoryType;
            HasFrontFacingBug = hasFrontFacingBug;
            HasVectorIndexingBug = hasVectorIndexingBug;
            NeedsFragmentOutputSpecialization = needsFragmentOutputSpecialization;
            ReduceShaderPrecision = reduceShaderPrecision;
            SupportsAstcCompression = supportsAstcCompression;
            SupportsBc123Compression = supportsBc123Compression;
            SupportsBc45Compression = supportsBc45Compression;
            SupportsBc67Compression = supportsBc67Compression;
            SupportsEtc2Compression = supportsEtc2Compression;
            Supports3DTextureCompression = supports3DTextureCompression;
            SupportsBgraFormat = supportsBgraFormat;
            SupportsR4G4Format = supportsR4G4Format;
            SupportsR4G4B4A4Format = supportsR4G4B4A4Format;
            SupportsScaledVertexFormats = supportsScaledVertexFormats;
            SupportsSnormBufferTextureFormat = supportsSnormBufferTextureFormat;
            Supports5BitComponentFormat = supports5BitComponentFormat;
            SupportsSparseBuffer = supportsSparseBuffer;
            SupportsBlendEquationAdvanced = supportsBlendEquationAdvanced;
            SupportsFragmentShaderInterlock = supportsFragmentShaderInterlock;
            SupportsFragmentShaderOrderingIntel = supportsFragmentShaderOrderingIntel;
            SupportsGeometryShader = supportsGeometryShader;
            SupportsGeometryShaderPassthrough = supportsGeometryShaderPassthrough;
            SupportsTransformFeedback = supportsTransformFeedback;
            SupportsImageLoadFormatted = supportsImageLoadFormatted;
            SupportsLayerVertexTessellation = supportsLayerVertexTessellation;
            SupportsMismatchingViewFormat = supportsMismatchingViewFormat;
            SupportsCubemapView = supportsCubemapView;
            SupportsNonConstantTextureOffset = supportsNonConstantTextureOffset;
            SupportsQuads = supportsQuads;
            SupportsSeparateSampler = supportsSeparateSampler;
            SupportsShaderBallot = supportsShaderBallot;
            SupportsShaderBarrierDivergence = supportsShaderBarrierDivergence;
            SupportsShaderFloat64 = supportsShaderFloat64;
            SupportsTextureGatherOffsets = supportsTextureGatherOffsets;
            SupportsTextureShadowLod = supportsTextureShadowLod;
            SupportsVertexStoreAndAtomics = supportsVertexStoreAndAtomics;
            SupportsViewportIndexVertexTessellation = supportsViewportIndexVertexTessellation;
            SupportsViewportMask = supportsViewportMask;
            SupportsViewportSwizzle = supportsViewportSwizzle;
            SupportsIndirectParameters = supportsIndirectParameters;
            SupportsDepthClipControl = supportsDepthClipControl;
            SupportsExtendedDynamicState = supportsExtendedDynamicState;
            SupportsExtendedDynamicState2 = supportsExtendedDynamicState2;
            SupportsLogicOpDynamicState = supportsLogicOpDynamicState;
            SupportsPatchControlPointsDynamicState = supportsPatchControlPointsDynamicState;
            UniformBufferSetIndex = uniformBufferSetIndex;
            StorageBufferSetIndex = storageBufferSetIndex;
            TextureSetIndex = textureSetIndex;
            ImageSetIndex = imageSetIndex;
            ExtraSetBaseIndex = extraSetBaseIndex;
            MaximumExtraSets = maximumExtraSets;
            MaximumUniformBuffersPerStage = maximumUniformBuffersPerStage;
            MaximumStorageBuffersPerStage = maximumStorageBuffersPerStage;
            MaximumTexturesPerStage = maximumTexturesPerStage;
            MaximumImagesPerStage = maximumImagesPerStage;
            MaximumComputeSharedMemorySize = maximumComputeSharedMemorySize;
            MaximumSupportedAnisotropy = maximumSupportedAnisotropy;
            ShaderSubgroupSize = shaderSubgroupSize;
            StorageBufferOffsetAlignment = storageBufferOffsetAlignment;
            TextureBufferOffsetAlignment = textureBufferOffsetAlignment;
            GatherBiasPrecision = gatherBiasPrecision;
            MaximumGpuMemory = maximumGpuMemory;
        }
    }
}
