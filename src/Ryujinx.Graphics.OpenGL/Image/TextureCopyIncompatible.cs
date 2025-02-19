using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

namespace Ryujinx.Graphics.OpenGL.Image
{
    class TextureCopyIncompatible
    {
        private const string ComputeShaderShortening = @"#version 450 core

layout (binding = 0, $SRC_FORMAT$) uniform uimage2D src;
layout (binding = 1, $DST_FORMAT$) uniform uimage2D dst;

layout (local_size_x = 32, local_size_y = 32, local_size_z = 1) in;

void main()
{
    uvec2 coords = gl_GlobalInvocationID.xy;
    ivec2 imageSz = imageSize(src);

    if (int(coords.x) >= imageSz.x || int(coords.y) >= imageSz.y)
    {
        return;
    }

    uint coordsShifted = coords.x << $RATIO_LOG2$;

    uvec2 dstCoords0 = uvec2(coordsShifted, coords.y);
    uvec2 dstCoords1 = uvec2(coordsShifted + 1, coords.y);
    uvec2 dstCoords2 = uvec2(coordsShifted + 2, coords.y);
    uvec2 dstCoords3 = uvec2(coordsShifted + 3, coords.y);

    uvec4 rgba = imageLoad(src, ivec2(coords));

    imageStore(dst, ivec2(dstCoords0), rgba.rrrr);
    imageStore(dst, ivec2(dstCoords1), rgba.gggg);
    imageStore(dst, ivec2(dstCoords2), rgba.bbbb);
    imageStore(dst, ivec2(dstCoords3), rgba.aaaa);
}";

        private const string ComputeShaderWidening = @"#version 450 core

layout (binding = 0, $SRC_FORMAT$) uniform uimage2D src;
layout (binding = 1, $DST_FORMAT$) uniform uimage2D dst;

layout (local_size_x = 32, local_size_y = 32, local_size_z = 1) in;

void main()
{
    uvec2 coords = gl_GlobalInvocationID.xy;
    ivec2 imageSz = imageSize(dst);

    if (int(coords.x) >= imageSz.x || int(coords.y) >= imageSz.y)
    {
        return;
    }

    uvec2 srcCoords = uvec2(coords.x << $RATIO_LOG2$, coords.y);

    uint r = imageLoad(src, ivec2(srcCoords) + ivec2(0, 0)).r;
    uint g = imageLoad(src, ivec2(srcCoords) + ivec2(1, 0)).r;
    uint b = imageLoad(src, ivec2(srcCoords) + ivec2(2, 0)).r;
    uint a = imageLoad(src, ivec2(srcCoords) + ivec2(3, 0)).r;

    imageStore(dst, ivec2(coords), uvec4(r, g, b, a));
}";

        private readonly OpenGLRenderer _renderer;
        private readonly Dictionary<int, int> _shorteningProgramHandles;
        private readonly Dictionary<int, int> _wideningProgramHandles;

        public TextureCopyIncompatible(OpenGLRenderer renderer)
        {
            _renderer = renderer;
            _shorteningProgramHandles = new Dictionary<int, int>();
            _wideningProgramHandles = new Dictionary<int, int>();
        }

        public void CopyIncompatibleFormats(ITextureInfo src, ITextureInfo dst, int srcLayer, int dstLayer, int srcLevel, int dstLevel, int depth, int levels)
        {
            int srcBpp = src.Info.BytesPerPixel;
            int dstBpp = dst.Info.BytesPerPixel;

            // Calculate ideal component size, given our constraints:
            // - Component size must not exceed bytes per pixel of source and destination image formats.
            // - Maximum component size is 4 (R32).
            int componentSize = Math.Min(Math.Min(srcBpp, dstBpp), 4);

            int srcComponentsCount = srcBpp / componentSize;
            int dstComponentsCount = dstBpp / componentSize;

            SizedInternalFormat srcFormat = GetFormat(componentSize, srcComponentsCount);
            SizedInternalFormat dstFormat = GetFormat(componentSize, dstComponentsCount);

            GL.UseProgram(srcBpp < dstBpp
                ? GetWideningShader(componentSize, srcComponentsCount, dstComponentsCount)
                : GetShorteningShader(componentSize, srcComponentsCount, dstComponentsCount));

            for (int l = 0; l < levels; l++)
            {
                int srcWidth = Math.Max(1, src.Info.Width >> l);
                int srcHeight = Math.Max(1, src.Info.Height >> l);

                int dstWidth = Math.Max(1, dst.Info.Width >> l);
                int dstHeight = Math.Max(1, dst.Info.Height >> l);

                int width = Math.Min(srcWidth, dstWidth);
                int height = Math.Min(srcHeight, dstHeight);

                for (int z = 0; z < depth; z++)
                {
                    GL.BindImageTexture(0, src.Handle, srcLevel + l, false, srcLayer + z, TextureAccess.ReadOnly, srcFormat);
                    GL.BindImageTexture(1, dst.Handle, dstLevel + l, false, dstLayer + z, TextureAccess.WriteOnly, dstFormat);

                    GL.DispatchCompute((width + 31) / 32, (height + 31) / 32, 1);
                }
            }

            Pipeline pipeline = (Pipeline)_renderer.Pipeline;

            pipeline.RestoreProgram();
            pipeline.RestoreImages1And2();
        }

        private static SizedInternalFormat GetFormat(int componentSize, int componentsCount)
        {
            if (componentSize == 1)
            {
                return componentsCount switch
                {
                    1 => SizedInternalFormat.R8ui,
                    2 => SizedInternalFormat.Rg8ui,
                    4 => SizedInternalFormat.Rgba8ui,
                    _ => throw new ArgumentException($"Invalid components count {componentsCount}."),
                };
            }
            else if (componentSize == 2)
            {
                return componentsCount switch
                {
                    1 => SizedInternalFormat.R16ui,
                    2 => SizedInternalFormat.Rg16ui,
                    4 => SizedInternalFormat.Rgba16ui,
                    _ => throw new ArgumentException($"Invalid components count {componentsCount}."),
                };
            }
            else if (componentSize == 4)
            {
                return componentsCount switch
                {
                    1 => SizedInternalFormat.R32ui,
                    2 => SizedInternalFormat.Rg32ui,
                    4 => SizedInternalFormat.Rgba32ui,
                    _ => throw new ArgumentException($"Invalid components count {componentsCount}."),
                };
            }
            else
            {
                throw new ArgumentException($"Invalid component size {componentSize}.");
            }
        }

        private int GetShorteningShader(int componentSize, int srcComponentsCount, int dstComponentsCount)
        {
            return GetShader(ComputeShaderShortening, _shorteningProgramHandles, componentSize, srcComponentsCount, dstComponentsCount);
        }

        private int GetWideningShader(int componentSize, int srcComponentsCount, int dstComponentsCount)
        {
            return GetShader(ComputeShaderWidening, _wideningProgramHandles, componentSize, srcComponentsCount, dstComponentsCount);
        }

        private static int GetShader(
            string code,
            Dictionary<int, int> programHandles,
            int componentSize,
            int srcComponentsCount,
            int dstComponentsCount)
        {
            int componentSizeLog2 = BitOperations.Log2((uint)componentSize);

            int srcIndex = componentSizeLog2 + BitOperations.Log2((uint)srcComponentsCount) * 3;
            int dstIndex = componentSizeLog2 + BitOperations.Log2((uint)dstComponentsCount) * 3;

            int key = srcIndex | (dstIndex << 8);

            if (!programHandles.TryGetValue(key, out int programHandle))
            {
                int csHandle = GL.CreateShader(ShaderType.ComputeShader);

                string[] formatTable = ["r8ui", "r16ui", "r32ui", "rg8ui", "rg16ui", "rg32ui", "rgba8ui", "rgba16ui", "rgba32ui"
                ];

                string srcFormat = formatTable[srcIndex];
                string dstFormat = formatTable[dstIndex];

                int srcBpp = srcComponentsCount * componentSize;
                int dstBpp = dstComponentsCount * componentSize;

                int ratio = srcBpp < dstBpp ? dstBpp / srcBpp : srcBpp / dstBpp;
                int ratioLog2 = BitOperations.Log2((uint)ratio);

                GL.ShaderSource(csHandle, code
                    .Replace("$SRC_FORMAT$", srcFormat)
                    .Replace("$DST_FORMAT$", dstFormat)
                    .Replace("$RATIO_LOG2$", ratioLog2.ToString(CultureInfo.InvariantCulture)));

                GL.CompileShader(csHandle);

                programHandle = GL.CreateProgram();

                GL.AttachShader(programHandle, csHandle);
                GL.LinkProgram(programHandle);
                GL.DetachShader(programHandle, csHandle);
                GL.DeleteShader(csHandle);

                GL.GetProgram(programHandle, GetProgramParameterName.LinkStatus, out int status);

                if (status == 0)
                {
                    throw new Exception(GL.GetProgramInfoLog(programHandle));
                }

                programHandles.Add(key, programHandle);
            }

            return programHandle;
        }

        public void Dispose()
        {
            foreach (int handle in _shorteningProgramHandles.Values)
            {
                GL.DeleteProgram(handle);
            }

            _shorteningProgramHandles.Clear();

            foreach (int handle in _wideningProgramHandles.Values)
            {
                GL.DeleteProgram(handle);
            }

            _wideningProgramHandles.Clear();
        }
    }
}
