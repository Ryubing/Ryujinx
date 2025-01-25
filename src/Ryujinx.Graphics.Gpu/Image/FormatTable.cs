using Ryujinx.Graphics.GAL;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Ryujinx.Graphics.Gpu.Image
{
    /// <summary>
    /// Contains format tables, for texture and vertex attribute formats.
    /// </summary>
    static class FormatTable
    {
#pragma warning disable IDE0055 // Disable formatting
        [SuppressMessage("Design", "CA1069: Enums values should not be duplicated")]
        private enum TextureFormat : uint
        {
            // Formats
            R32G32B32A32 = 0x01,
            R32G32B32 = 0x02,
            R16G16B16A16 = 0x03,
            R32G32 = 0x04,
            R32B24G8 = 0x05,
            X8B8G8R8 = 0x07,
            A8B8G8R8 = 0x08,
            A2B10G10R10 = 0x09,
            R16G16 = 0x0c,
            G8R24 = 0x0d,
            G24R8 = 0x0e,
            R32 = 0x0f,
            A4B4G4R4 = 0x12,
            A5B5G5R1 = 0x13,
            A1B5G5R5 = 0x14,
            B5G6R5 = 0x15,
            B6G5R5 = 0x16,
            G8R8 = 0x18,
            R16 = 0x1b,
            Y8Video = 0x1c,
            R8 = 0x1d,
            G4R4 = 0x1e,
            R1 = 0x1f,
            E5B9G9R9SharedExp = 0x20,
            Bf10Gf11Rf11 = 0x21,
            G8B8G8R8 = 0x22,
            B8G8R8G8 = 0x23,
            Bc1 = 0x24,
            Bc2 = 0x25,
            Bc3 = 0x26,
            Bc4 = 0x27,
            Bc5 = 0x28,
            Bc6HSf16 = 0x10,
            Bc6HUf16 = 0x11,
            Bc7U = 0x17,
            Etc2Rgb = 0x06,
            Etc2RgbPta = 0x0a,
            Etc2Rgba = 0x0b,
            Eac = 0x19,
            Eacx2 = 0x1a,
            Z24S8 = 0x29,
            X8Z24 = 0x2a,
            S8Z24 = 0x2b,
            X4V4Z24Cov4R4V = 0x2c,
            X4V4Z24Cov8R8V = 0x2d,
            V8Z24Cov4R12V = 0x2e,
            Zf32 = 0x2f,
            Zf32X24S8 = 0x30,
            X8Z24X20V4S8Cov4R4V = 0x31,
            X8Z24X20V4S8Cov8R8V = 0x32,
            Zf32X20V4X8Cov4R4V = 0x33,
            Zf32X20V4X8Cov8R8V = 0x34,
            Zf32X20V4S8Cov4R4V = 0x35,
            Zf32X20V4S8Cov8R8V = 0x36,
            X8Z24X16V8S8Cov4R12V = 0x37,
            Zf32X16V8X8Cov4R12V = 0x38,
            Zf32X16V8S8Cov4R12V = 0x39,
            Z16 = 0x3a,
            V8Z24Cov8R24V = 0x3b,
            X8Z24X16V8S8Cov8R24V = 0x3c,
            Zf32X16V8X8Cov8R24V = 0x3d,
            Zf32X16V8S8Cov8R24V = 0x3e,
            Astc2D4x4 = 0x40,
            Astc2D5x4 = 0x50,
            Astc2D5x5 = 0x41,
            Astc2D6x5 = 0x51,
            Astc2D6x6 = 0x42,
            Astc2D8x5 = 0x55,
            Astc2D8x6 = 0x52,
            Astc2D8x8 = 0x44,
            Astc2D10x5 = 0x56,
            Astc2D10x6 = 0x57,
            Astc2D10x8 = 0x53,
            Astc2D10x10 = 0x45,
            Astc2D12x10 = 0x54,
            Astc2D12x12 = 0x46,

            // Types
            Snorm = 0x1,
            Unorm = 0x2,
            Sint = 0x3,
            Uint = 0x4,
            SnormForceFp16 = 0x5,
            UnormForceFp16 = 0x6,
            Float = 0x7,

            // Component Types
            RSnorm = Snorm << 7,
            GSnorm = Snorm << 10,
            BSnorm = Snorm << 13,
            ASnorm = Snorm << 16,

            RUnorm = Unorm << 7,
            GUnorm = Unorm << 10,
            BUnorm = Unorm << 13,
            AUnorm = Unorm << 16,

            RSint = Sint << 7,
            GSint = Sint << 10,
            BSint = Sint << 13,
            ASint = Sint << 16,

            RUint = Uint << 7,
            GUint = Uint << 10,
            BUint = Uint << 13,
            AUint = Uint << 16,

            RSnormForceFp16 = SnormForceFp16 << 7,
            GSnormForceFp16 = SnormForceFp16 << 10,
            BSnormForceFp16 = SnormForceFp16 << 13,
            ASnormForceFp16 = SnormForceFp16 << 16,

            RUnormForceFp16 = UnormForceFp16 << 7,
            GUnormForceFp16 = UnormForceFp16 << 10,
            BUnormForceFp16 = UnormForceFp16 << 13,
            AUnormForceFp16 = UnormForceFp16 << 16,

            RFloat = Float << 7,
            GFloat = Float << 10,
            BFloat = Float << 13,
            AFloat = Float << 16,

            Srgb = 0x1 << 19, // Custom encoding

            // Combinations
            R8Unorm                          = R8                | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x2491d
            R8Snorm                          = R8                | RSnorm | GSnorm | BSnorm | ASnorm,        // 0x1249d
            R8Uint                           = R8                | RUint  | GUint  | BUint  | AUint,         // 0x4921d
            R8Sint                           = R8                | RSint  | GSint  | BSint  | ASint,         // 0x36d9d
            R16Float                         = R16               | RFloat | GFloat | BFloat | AFloat,        // 0x7ff9b
            R16Unorm                         = R16               | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x2491b
            R16Snorm                         = R16               | RSnorm | GSnorm | BSnorm | ASnorm,        // 0x1249b
            R16Uint                          = R16               | RUint  | GUint  | BUint  | AUint,         // 0x4921b
            R16Sint                          = R16               | RSint  | GSint  | BSint  | ASint,         // 0x36d9b
            R32Float                         = R32               | RFloat | GFloat | BFloat | AFloat,        // 0x7ff8f
            R32Uint                          = R32               | RUint  | GUint  | BUint  | AUint,         // 0x4920f
            R32Sint                          = R32               | RSint  | GSint  | BSint  | ASint,         // 0x36d8f
            G8R8Unorm                        = G8R8              | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x24918
            G8R8Snorm                        = G8R8              | RSnorm | GSnorm | BSnorm | ASnorm,        // 0x12498
            G8R8Uint                         = G8R8              | RUint  | GUint  | BUint  | AUint,         // 0x49218
            G8R8Sint                         = G8R8              | RSint  | GSint  | BSint  | ASint,         // 0x36d98
            R16G16Float                      = R16G16            | RFloat | GFloat | BFloat | AFloat,        // 0x7ff8c
            R16G16Unorm                      = R16G16            | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x2490c
            R16G16Snorm                      = R16G16            | RSnorm | GSnorm | BSnorm | ASnorm,        // 0x1248c
            R16G16Uint                       = R16G16            | RUint  | GUint  | BUint  | AUint,         // 0x4920c
            R16G16Sint                       = R16G16            | RSint  | GSint  | BSint  | ASint,         // 0x36d8c
            R32G32Float                      = R32G32            | RFloat | GFloat | BFloat | AFloat,        // 0x7ff84
            R32G32Uint                       = R32G32            | RUint  | GUint  | BUint  | AUint,         // 0x49204
            R32G32Sint                       = R32G32            | RSint  | GSint  | BSint  | ASint,         // 0x36d84
            R32G32B32Float                   = R32G32B32         | RFloat | GFloat | BFloat | AFloat,        // 0x7ff82
            R32G32B32Uint                    = R32G32B32         | RUint  | GUint  | BUint  | AUint,         // 0x49202
            R32G32B32Sint                    = R32G32B32         | RSint  | GSint  | BSint  | ASint,         // 0x36d82
            A8B8G8R8Unorm                    = A8B8G8R8          | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x24908
            A8B8G8R8Snorm                    = A8B8G8R8          | RSnorm | GSnorm | BSnorm | ASnorm,        // 0x12488
            A8B8G8R8Uint                     = A8B8G8R8          | RUint  | GUint  | BUint  | AUint,         // 0x49208
            A8B8G8R8Sint                     = A8B8G8R8          | RSint  | GSint  | BSint  | ASint,         // 0x36d88
            R16G16B16A16Float                = R16G16B16A16      | RFloat | GFloat | BFloat | AFloat,        // 0x7ff83
            R16G16B16A16Unorm                = R16G16B16A16      | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x24903
            R16G16B16A16Snorm                = R16G16B16A16      | RSnorm | GSnorm | BSnorm | ASnorm,        // 0x12483
            R16G16B16A16Uint                 = R16G16B16A16      | RUint  | GUint  | BUint  | AUint,         // 0x49203
            R16G16B16A16Sint                 = R16G16B16A16      | RSint  | GSint  | BSint  | ASint,         // 0x36d83
            R32G32B32A32Float                = R32G32B32A32      | RFloat | GFloat | BFloat | AFloat,        // 0x7ff81
            R32G32B32A32Uint                 = R32G32B32A32      | RUint  | GUint  | BUint  | AUint,         // 0x49201
            R32G32B32A32Sint                 = R32G32B32A32      | RSint  | GSint  | BSint  | ASint,         // 0x36d81
            Z16Unorm                         = Z16               | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x2493a
            Z16RUnormGUintBUintAUint         = Z16               | RUnorm | GUint  | BUint  | AUint,         // 0x4913a
            Zf32RFloatGUintBUintAUint        = Zf32              | RFloat | GUint  | BUint  | AUint,         // 0x493af
            Zf32Float                        = Zf32              | RFloat | GFloat | BFloat | AFloat,        // 0x7ffaf
            G24R8RUintGUnormBUnormAUnorm     = G24R8             | RUint  | GUnorm | BUnorm | AUnorm,        // 0x24a0e
            Z24S8RUintGUnormBUnormAUnorm     = Z24S8             | RUint  | GUnorm | BUnorm | AUnorm,        // 0x24a29
            Z24S8RUintGUnormBUintAUint       = Z24S8             | RUint  | GUnorm | BUint  | AUint,         // 0x48a29
            X8Z24RUnormGUintBUintAUint       = X8Z24             | RUnorm | GUint  | BUint  | AUint,         // 0x4912a
            S8Z24RUnormGUintBUintAUint       = S8Z24             | RUnorm | GUint  | BUint  | AUint,         // 0x4912b
            R32B24G8RFloatGUintBUnormAUnorm  = R32B24G8          | RFloat | GUint  | BUnorm | AUnorm,        // 0x25385
            Zf32X24S8RFloatGUintBUnormAUnorm = Zf32X24S8         | RFloat | GUint  | BUnorm | AUnorm,        // 0x253b0
            A8B8G8R8UnormSrgb                = A8B8G8R8          | RUnorm | GUnorm | BUnorm | AUnorm | Srgb, // 0xa4908
            G4R4Unorm                        = G4R4              | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x2491e
            A4B4G4R4Unorm                    = A4B4G4R4          | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x24912
            A1B5G5R5Unorm                    = A1B5G5R5          | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x24914
            B5G6R5Unorm                      = B5G6R5            | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x24915
            A2B10G10R10Unorm                 = A2B10G10R10       | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x24909
            A2B10G10R10Uint                  = A2B10G10R10       | RUint  | GUint  | BUint  | AUint,         // 0x49209
            Bf10Gf11Rf11Float                = Bf10Gf11Rf11      | RFloat | GFloat | BFloat | AFloat,        // 0x7ffa1
            E5B9G9R9SharedExpFloat           = E5B9G9R9SharedExp | RFloat | GFloat | BFloat | AFloat,        // 0x7ffa0
            Bc1Unorm                         = Bc1               | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x24924
            Bc2Unorm                         = Bc2               | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x24925
            Bc3Unorm                         = Bc3               | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x24926
            Bc1UnormSrgb                     = Bc1               | RUnorm | GUnorm | BUnorm | AUnorm | Srgb, // 0xa4924
            Bc2UnormSrgb                     = Bc2               | RUnorm | GUnorm | BUnorm | AUnorm | Srgb, // 0xa4925
            Bc3UnormSrgb                     = Bc3               | RUnorm | GUnorm | BUnorm | AUnorm | Srgb, // 0xa4926
            Bc4Unorm                         = Bc4               | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x24927
            Bc4Snorm                         = Bc4               | RSnorm | GSnorm | BSnorm | ASnorm,        // 0x124a7
            Bc5Unorm                         = Bc5               | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x24928
            Bc5Snorm                         = Bc5               | RSnorm | GSnorm | BSnorm | ASnorm,        // 0x124a8
            Bc7UUnorm                        = Bc7U              | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x24917
            Bc7UUnormSrgb                    = Bc7U              | RUnorm | GUnorm | BUnorm | AUnorm | Srgb, // 0xa4917
            Bc6HSf16Float                    = Bc6HSf16          | RFloat | GFloat | BFloat | AFloat,        // 0x7ff90
            Bc6HUf16Float                    = Bc6HUf16          | RFloat | GFloat | BFloat | AFloat,        // 0x7ff91
            Etc2RgbUnorm                     = Etc2Rgb           | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x24906
            Etc2RgbPtaUnorm                  = Etc2RgbPta        | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x2490a
            Etc2RgbaUnorm                    = Etc2Rgba          | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x2490b
            Etc2RgbUnormSrgb                 = Etc2Rgb           | RUnorm | GUnorm | BUnorm | AUnorm | Srgb, // 0xa4906
            Etc2RgbPtaUnormSrgb              = Etc2RgbPta        | RUnorm | GUnorm | BUnorm | AUnorm | Srgb, // 0xa490a
            Etc2RgbaUnormSrgb                = Etc2Rgba          | RUnorm | GUnorm | BUnorm | AUnorm | Srgb, // 0xa490b
            Astc2D4x4Unorm                   = Astc2D4x4         | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x24940
            Astc2D5x4Unorm                   = Astc2D5x4         | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x24950
            Astc2D5x5Unorm                   = Astc2D5x5         | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x24941
            Astc2D6x5Unorm                   = Astc2D6x5         | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x24951
            Astc2D6x6Unorm                   = Astc2D6x6         | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x24942
            Astc2D8x5Unorm                   = Astc2D8x5         | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x24955
            Astc2D8x6Unorm                   = Astc2D8x6         | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x24952
            Astc2D8x8Unorm                   = Astc2D8x8         | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x24944
            Astc2D10x5Unorm                  = Astc2D10x5        | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x24956
            Astc2D10x6Unorm                  = Astc2D10x6        | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x24957
            Astc2D10x8Unorm                  = Astc2D10x8        | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x24953
            Astc2D10x10Unorm                 = Astc2D10x10       | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x24945
            Astc2D12x10Unorm                 = Astc2D12x10       | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x24954
            Astc2D12x12Unorm                 = Astc2D12x12       | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x24946
            Astc2D4x4UnormSrgb               = Astc2D4x4         | RUnorm | GUnorm | BUnorm | AUnorm | Srgb, // 0xa4940
            Astc2D5x4UnormSrgb               = Astc2D5x4         | RUnorm | GUnorm | BUnorm | AUnorm | Srgb, // 0xa4950
            Astc2D5x5UnormSrgb               = Astc2D5x5         | RUnorm | GUnorm | BUnorm | AUnorm | Srgb, // 0xa4941
            Astc2D6x5UnormSrgb               = Astc2D6x5         | RUnorm | GUnorm | BUnorm | AUnorm | Srgb, // 0xa4951
            Astc2D6x6UnormSrgb               = Astc2D6x6         | RUnorm | GUnorm | BUnorm | AUnorm | Srgb, // 0xa4942
            Astc2D8x5UnormSrgb               = Astc2D8x5         | RUnorm | GUnorm | BUnorm | AUnorm | Srgb, // 0xa4955
            Astc2D8x6UnormSrgb               = Astc2D8x6         | RUnorm | GUnorm | BUnorm | AUnorm | Srgb, // 0xa4952
            Astc2D8x8UnormSrgb               = Astc2D8x8         | RUnorm | GUnorm | BUnorm | AUnorm | Srgb, // 0xa4944
            Astc2D10x5UnormSrgb              = Astc2D10x5        | RUnorm | GUnorm | BUnorm | AUnorm | Srgb, // 0xa4956
            Astc2D10x6UnormSrgb              = Astc2D10x6        | RUnorm | GUnorm | BUnorm | AUnorm | Srgb, // 0xa4957
            Astc2D10x8UnormSrgb              = Astc2D10x8        | RUnorm | GUnorm | BUnorm | AUnorm | Srgb, // 0xa4953
            Astc2D10x10UnormSrgb             = Astc2D10x10       | RUnorm | GUnorm | BUnorm | AUnorm | Srgb, // 0xa4945
            Astc2D12x10UnormSrgb             = Astc2D12x10       | RUnorm | GUnorm | BUnorm | AUnorm | Srgb, // 0xa4954
            Astc2D12x12UnormSrgb             = Astc2D12x12       | RUnorm | GUnorm | BUnorm | AUnorm | Srgb, // 0xa4946
            A5B5G5R1Unorm                    = A5B5G5R1          | RUnorm | GUnorm | BUnorm | AUnorm,        // 0x24913
        }

        [SuppressMessage("Design", "CA1069: Enums values should not be duplicated")]
        private enum VertexAttributeFormat : uint
        {
            // Width
            R32G32B32A32 = 0x01,
            R32G32B32 = 0x02,
            R16G16B16A16 = 0x03,
            R32G32 = 0x04,
            R16G16B16 = 0x05,
            A8B8G8R8 = 0x2f,
            R8G8B8A8 = 0x0a,
            X8B8G8R8 = 0x33,
            A2B10G10R10 = 0x30,
            B10G11R11 = 0x31,
            R16G16 = 0x0f,
            R32 = 0x12,
            R8G8B8 = 0x13,
            G8R8 = 0x32,
            R8G8 = 0x18,
            R16 = 0x1b,
            R8 = 0x1d,
            A8 = 0x34,

            // Type
            Snorm = 0x01,
            Unorm = 0x02,
            Sint = 0x03,
            Uint = 0x04,
            Uscaled = 0x05,
            Sscaled = 0x06,
            Float = 0x07,

            // Combinations
            R8Unorm             = (R8 << 21)           | (Unorm << 27),   // 0x13a00000
            R8Snorm             = (R8 << 21)           | (Snorm << 27),   // 0x0ba00000
            R8Uint              = (R8 << 21)           | (Uint << 27),    // 0x23a00000
            R8Sint              = (R8 << 21)           | (Sint << 27),    // 0x1ba00000
            R16Float            = (R16 << 21)          | (Float << 27),   // 0x3b600000
            R16Unorm            = (R16 << 21)          | (Unorm << 27),   // 0x13600000
            R16Snorm            = (R16 << 21)          | (Snorm << 27),   // 0x0b600000
            R16Uint             = (R16 << 21)          | (Uint << 27),    // 0x23600000
            R16Sint             = (R16 << 21)          | (Sint << 27),    // 0x1b600000
            R32Float            = (R32 << 21)          | (Float << 27),   // 0x3a400000
            R32Uint             = (R32 << 21)          | (Uint << 27),    // 0x22400000
            R32Sint             = (R32 << 21)          | (Sint << 27),    // 0x1a400000
            R8G8Unorm           = (R8G8 << 21)         | (Unorm << 27),   // 0x13000000
            R8G8Snorm           = (R8G8 << 21)         | (Snorm << 27),   // 0x0b000000
            R8G8Uint            = (R8G8 << 21)         | (Uint << 27),    // 0x23000000
            R8G8Sint            = (R8G8 << 21)         | (Sint << 27),    // 0x1b000000
            R16G16Float         = (R16G16 << 21)       | (Float << 27),   // 0x39e00000
            R16G16Unorm         = (R16G16 << 21)       | (Unorm << 27),   // 0x11e00000
            R16G16Snorm         = (R16G16 << 21)       | (Snorm << 27),   // 0x09e00000
            R16G16Uint          = (R16G16 << 21)       | (Uint << 27),    // 0x21e00000
            R16G16Sint          = (R16G16 << 21)       | (Sint << 27),    // 0x19e00000
            R32G32Float         = (R32G32 << 21)       | (Float << 27),   // 0x38800000
            R32G32Uint          = (R32G32 << 21)       | (Uint << 27),    // 0x20800000
            R32G32Sint          = (R32G32 << 21)       | (Sint << 27),    // 0x18800000
            R8G8B8Unorm         = (R8G8B8 << 21)       | (Unorm << 27),   // 0x12600000
            R8G8B8Snorm         = (R8G8B8 << 21)       | (Snorm << 27),   // 0x0a600000
            R8G8B8Uint          = (R8G8B8 << 21)       | (Uint << 27),    // 0x22600000
            R8G8B8Sint          = (R8G8B8 << 21)       | (Sint << 27),    // 0x1a600000
            R16G16B16Float      = (R16G16B16 << 21)    | (Float << 27),   // 0x38a00000
            R16G16B16Unorm      = (R16G16B16 << 21)    | (Unorm << 27),   // 0x10a00000
            R16G16B16Snorm      = (R16G16B16 << 21)    | (Snorm << 27),   // 0x08a00000
            R16G16B16Uint       = (R16G16B16 << 21)    | (Uint << 27),    // 0x20a00000
            R16G16B16Sint       = (R16G16B16 << 21)    | (Sint << 27),    // 0x18a00000
            R32G32B32Float      = (R32G32B32 << 21)    | (Float << 27),   // 0x38400000
            R32G32B32Uint       = (R32G32B32 << 21)    | (Uint << 27),    // 0x20400000
            R32G32B32Sint       = (R32G32B32 << 21)    | (Sint << 27),    // 0x18400000
            R8G8B8A8Unorm       = (R8G8B8A8 << 21)     | (Unorm << 27),   // 0x11400000
            R8G8B8A8Snorm       = (R8G8B8A8 << 21)     | (Snorm << 27),   // 0x09400000
            R8G8B8A8Uint        = (R8G8B8A8 << 21)     | (Uint << 27),    // 0x21400000
            R8G8B8A8Sint        = (R8G8B8A8 << 21)     | (Sint << 27),    // 0x19400000
            R16G16B16A16Float   = (R16G16B16A16 << 21) | (Float << 27),   // 0x38600000
            R16G16B16A16Unorm   = (R16G16B16A16 << 21) | (Unorm << 27),   // 0x10600000
            R16G16B16A16Snorm   = (R16G16B16A16 << 21) | (Snorm << 27),   // 0x08600000
            R16G16B16A16Uint    = (R16G16B16A16 << 21) | (Uint << 27),    // 0x20600000
            R16G16B16A16Sint    = (R16G16B16A16 << 21) | (Sint << 27),    // 0x18600000
            R32G32B32A32Float   = (R32G32B32A32 << 21) | (Float << 27),   // 0x38200000
            R32G32B32A32Uint    = (R32G32B32A32 << 21) | (Uint << 27),    // 0x20200000
            R32G32B32A32Sint    = (R32G32B32A32 << 21) | (Sint << 27),    // 0x18200000
            A2B10G10R10Unorm    = (A2B10G10R10 << 21)  | (Unorm << 27),   // 0x16000000
            A2B10G10R10Uint     = (A2B10G10R10 << 21)  | (Uint << 27),    // 0x26000000
            B10G11R11Float      = (B10G11R11 << 21)    | (Float << 27),   // 0x3e200000
            R8Uscaled           = (R8 << 21)           | (Uscaled << 27), // 0x2ba00000
            R8Sscaled           = (R8 << 21)           | (Sscaled << 27), // 0x33a00000
            R16Uscaled          = (R16 << 21)          | (Uscaled << 27), // 0x2b600000
            R16Sscaled          = (R16 << 21)          | (Sscaled << 27), // 0x33600000
            R32Uscaled          = (R32 << 21)          | (Uscaled << 27), // 0x2a400000
            R32Sscaled          = (R32 << 21)          | (Sscaled << 27), // 0x32400000
            R8G8Uscaled         = (R8G8 << 21)         | (Uscaled << 27), // 0x2b000000
            R8G8Sscaled         = (R8G8 << 21)         | (Sscaled << 27), // 0x33000000
            R16G16Uscaled       = (R16G16 << 21)       | (Uscaled << 27), // 0x29e00000
            R16G16Sscaled       = (R16G16 << 21)       | (Sscaled << 27), // 0x31e00000
            R32G32Uscaled       = (R32G32 << 21)       | (Uscaled << 27), // 0x28800000
            R32G32Sscaled       = (R32G32 << 21)       | (Sscaled << 27), // 0x30800000
            R8G8B8Uscaled       = (R8G8B8 << 21)       | (Uscaled << 27), // 0x2a600000
            R8G8B8Sscaled       = (R8G8B8 << 21)       | (Sscaled << 27), // 0x32600000
            R16G16B16Uscaled    = (R16G16B16 << 21)    | (Uscaled << 27), // 0x28a00000
            R16G16B16Sscaled    = (R16G16B16 << 21)    | (Sscaled << 27), // 0x30a00000
            R32G32B32Uscaled    = (R32G32B32 << 21)    | (Uscaled << 27), // 0x28400000
            R32G32B32Sscaled    = (R32G32B32 << 21)    | (Sscaled << 27), // 0x30400000
            R8G8B8A8Uscaled     = (R8G8B8A8 << 21)     | (Uscaled << 27), // 0x29400000
            R8G8B8A8Sscaled     = (R8G8B8A8 << 21)     | (Sscaled << 27), // 0x31400000
            R16G16B16A16Uscaled = (R16G16B16A16 << 21) | (Uscaled << 27), // 0x28600000
            R16G16B16A16Sscaled = (R16G16B16A16 << 21) | (Sscaled << 27), // 0x30600000
            R32G32B32A32Uscaled = (R32G32B32A32 << 21) | (Uscaled << 27), // 0x28200000
            R32G32B32A32Sscaled = (R32G32B32A32 << 21) | (Sscaled << 27), // 0x30200000
            A2B10G10R10Snorm    = (A2B10G10R10 << 21)  | (Snorm << 27),   // 0x0e000000
            A2B10G10R10Sint     = (A2B10G10R10 << 21)  | (Sint << 27),    // 0x1e000000
            A2B10G10R10Uscaled  = (A2B10G10R10 << 21)  | (Uscaled << 27), // 0x2e000000
            A2B10G10R10Sscaled  = (A2B10G10R10 << 21)  | (Sscaled << 27), // 0x36000000
        }

        private static readonly Dictionary<TextureFormat, FormatInfo> _textureFormats = new()
        {
            { TextureFormat.R8Unorm,                          new FormatInfo(Format.R8Unorm,           1,  1,  1,  1) },
            { TextureFormat.R8Snorm,                          new FormatInfo(Format.R8Snorm,           1,  1,  1,  1) },
            { TextureFormat.R8Uint,                           new FormatInfo(Format.R8Uint,            1,  1,  1,  1) },
            { TextureFormat.R8Sint,                           new FormatInfo(Format.R8Sint,            1,  1,  1,  1) },
            { TextureFormat.R16Float,                         new FormatInfo(Format.R16Float,          1,  1,  2,  1) },
            { TextureFormat.R16Unorm,                         new FormatInfo(Format.R16Unorm,          1,  1,  2,  1) },
            { TextureFormat.R16Snorm,                         new FormatInfo(Format.R16Snorm,          1,  1,  2,  1) },
            { TextureFormat.R16Uint,                          new FormatInfo(Format.R16Uint,           1,  1,  2,  1) },
            { TextureFormat.R16Sint,                          new FormatInfo(Format.R16Sint,           1,  1,  2,  1) },
            { TextureFormat.R32Float,                         new FormatInfo(Format.R32Float,          1,  1,  4,  1) },
            { TextureFormat.R32Uint,                          new FormatInfo(Format.R32Uint,           1,  1,  4,  1) },
            { TextureFormat.R32Sint,                          new FormatInfo(Format.R32Sint,           1,  1,  4,  1) },
            { TextureFormat.G8R8Unorm,                        new FormatInfo(Format.R8G8Unorm,         1,  1,  2,  2) },
            { TextureFormat.G8R8Snorm,                        new FormatInfo(Format.R8G8Snorm,         1,  1,  2,  2) },
            { TextureFormat.G8R8Uint,                         new FormatInfo(Format.R8G8Uint,          1,  1,  2,  2) },
            { TextureFormat.G8R8Sint,                         new FormatInfo(Format.R8G8Sint,          1,  1,  2,  2) },
            { TextureFormat.R16G16Float,                      new FormatInfo(Format.R16G16Float,       1,  1,  4,  2) },
            { TextureFormat.R16G16Unorm,                      new FormatInfo(Format.R16G16Unorm,       1,  1,  4,  2) },
            { TextureFormat.R16G16Snorm,                      new FormatInfo(Format.R16G16Snorm,       1,  1,  4,  2) },
            { TextureFormat.R16G16Uint,                       new FormatInfo(Format.R16G16Uint,        1,  1,  4,  2) },
            { TextureFormat.R16G16Sint,                       new FormatInfo(Format.R16G16Sint,        1,  1,  4,  2) },
            { TextureFormat.R32G32Float,                      new FormatInfo(Format.R32G32Float,       1,  1,  8,  2) },
            { TextureFormat.R32G32Uint,                       new FormatInfo(Format.R32G32Uint,        1,  1,  8,  2) },
            { TextureFormat.R32G32Sint,                       new FormatInfo(Format.R32G32Sint,        1,  1,  8,  2) },
            { TextureFormat.R32G32B32Float,                   new FormatInfo(Format.R32G32B32Float,    1,  1,  12, 3) },
            { TextureFormat.R32G32B32Uint,                    new FormatInfo(Format.R32G32B32Uint,     1,  1,  12, 3) },
            { TextureFormat.R32G32B32Sint,                    new FormatInfo(Format.R32G32B32Sint,     1,  1,  12, 3) },
            { TextureFormat.A8B8G8R8Unorm,                    new FormatInfo(Format.R8G8B8A8Unorm,     1,  1,  4,  4) },
            { TextureFormat.A8B8G8R8Snorm,                    new FormatInfo(Format.R8G8B8A8Snorm,     1,  1,  4,  4) },
            { TextureFormat.A8B8G8R8Uint,                     new FormatInfo(Format.R8G8B8A8Uint,      1,  1,  4,  4) },
            { TextureFormat.A8B8G8R8Sint,                     new FormatInfo(Format.R8G8B8A8Sint,      1,  1,  4,  4) },
            { TextureFormat.R16G16B16A16Float,                new FormatInfo(Format.R16G16B16A16Float, 1,  1,  8,  4) },
            { TextureFormat.R16G16B16A16Unorm,                new FormatInfo(Format.R16G16B16A16Unorm, 1,  1,  8,  4) },
            { TextureFormat.R16G16B16A16Snorm,                new FormatInfo(Format.R16G16B16A16Snorm, 1,  1,  8,  4) },
            { TextureFormat.R16G16B16A16Uint,                 new FormatInfo(Format.R16G16B16A16Uint,  1,  1,  8,  4) },
            { TextureFormat.R16G16B16A16Sint,                 new FormatInfo(Format.R16G16B16A16Sint,  1,  1,  8,  4) },
            { TextureFormat.R32G32B32A32Float,                new FormatInfo(Format.R32G32B32A32Float, 1,  1,  16, 4) },
            { TextureFormat.R32G32B32A32Uint,                 new FormatInfo(Format.R32G32B32A32Uint,  1,  1,  16, 4) },
            { TextureFormat.R32G32B32A32Sint,                 new FormatInfo(Format.R32G32B32A32Sint,  1,  1,  16, 4) },
            { TextureFormat.Z16Unorm,                         new FormatInfo(Format.D16Unorm,          1,  1,  2,  1) },
            { TextureFormat.Z16RUnormGUintBUintAUint,         new FormatInfo(Format.D16Unorm,          1,  1,  2,  1) },
            { TextureFormat.Zf32RFloatGUintBUintAUint,        new FormatInfo(Format.D32Float,          1,  1,  4,  1) },
            { TextureFormat.Zf32Float,                        new FormatInfo(Format.D32Float,          1,  1,  4,  1) },
            { TextureFormat.G24R8RUintGUnormBUnormAUnorm,     new FormatInfo(Format.D24UnormS8Uint,    1,  1,  4,  2) },
            { TextureFormat.Z24S8RUintGUnormBUnormAUnorm,     new FormatInfo(Format.D24UnormS8Uint,    1,  1,  4,  2) },
            { TextureFormat.Z24S8RUintGUnormBUintAUint,       new FormatInfo(Format.D24UnormS8Uint,    1,  1,  4,  2) },
            { TextureFormat.X8Z24RUnormGUintBUintAUint,       new FormatInfo(Format.X8UintD24Unorm,    1,  1,  4,  2) },
            { TextureFormat.S8Z24RUnormGUintBUintAUint,       new FormatInfo(Format.S8UintD24Unorm,    1,  1,  4,  2) },
            { TextureFormat.R32B24G8RFloatGUintBUnormAUnorm,  new FormatInfo(Format.D32FloatS8Uint,    1,  1,  8,  2) },
            { TextureFormat.Zf32X24S8RFloatGUintBUnormAUnorm, new FormatInfo(Format.D32FloatS8Uint,    1,  1,  8,  2) },
            { TextureFormat.A8B8G8R8UnormSrgb,                new FormatInfo(Format.R8G8B8A8Srgb,      1,  1,  4,  4) },
            { TextureFormat.G4R4Unorm,                        new FormatInfo(Format.R4G4Unorm,         1,  1,  1,  2) },
            { TextureFormat.A4B4G4R4Unorm,                    new FormatInfo(Format.R4G4B4A4Unorm,     1,  1,  2,  4) },
            { TextureFormat.A1B5G5R5Unorm,                    new FormatInfo(Format.R5G5B5A1Unorm,     1,  1,  2,  4) },
            { TextureFormat.B5G6R5Unorm,                      new FormatInfo(Format.R5G6B5Unorm,       1,  1,  2,  3) },
            { TextureFormat.A2B10G10R10Unorm,                 new FormatInfo(Format.R10G10B10A2Unorm,  1,  1,  4,  4) },
            { TextureFormat.A2B10G10R10Uint,                  new FormatInfo(Format.R10G10B10A2Uint,   1,  1,  4,  4) },
            { TextureFormat.Bf10Gf11Rf11Float,                new FormatInfo(Format.R11G11B10Float,    1,  1,  4,  3) },
            { TextureFormat.E5B9G9R9SharedExpFloat,           new FormatInfo(Format.R9G9B9E5Float,     1,  1,  4,  4) },
            { TextureFormat.Bc1Unorm,                         new FormatInfo(Format.Bc1RgbaUnorm,      4,  4,  8,  4) },
            { TextureFormat.Bc2Unorm,                         new FormatInfo(Format.Bc2Unorm,          4,  4,  16, 4) },
            { TextureFormat.Bc3Unorm,                         new FormatInfo(Format.Bc3Unorm,          4,  4,  16, 4) },
            { TextureFormat.Bc1UnormSrgb,                     new FormatInfo(Format.Bc1RgbaSrgb,       4,  4,  8,  4) },
            { TextureFormat.Bc2UnormSrgb,                     new FormatInfo(Format.Bc2Srgb,           4,  4,  16, 4) },
            { TextureFormat.Bc3UnormSrgb,                     new FormatInfo(Format.Bc3Srgb,           4,  4,  16, 4) },
            { TextureFormat.Bc4Unorm,                         new FormatInfo(Format.Bc4Unorm,          4,  4,  8,  1) },
            { TextureFormat.Bc4Snorm,                         new FormatInfo(Format.Bc4Snorm,          4,  4,  8,  1) },
            { TextureFormat.Bc5Unorm,                         new FormatInfo(Format.Bc5Unorm,          4,  4,  16, 2) },
            { TextureFormat.Bc5Snorm,                         new FormatInfo(Format.Bc5Snorm,          4,  4,  16, 2) },
            { TextureFormat.Bc7UUnorm,                        new FormatInfo(Format.Bc7Unorm,          4,  4,  16, 4) },
            { TextureFormat.Bc7UUnormSrgb,                    new FormatInfo(Format.Bc7Srgb,           4,  4,  16, 4) },
            { TextureFormat.Bc6HSf16Float,                    new FormatInfo(Format.Bc6HSfloat,        4,  4,  16, 4) },
            { TextureFormat.Bc6HUf16Float,                    new FormatInfo(Format.Bc6HUfloat,        4,  4,  16, 4) },
            { TextureFormat.Etc2RgbUnorm,                     new FormatInfo(Format.Etc2RgbUnorm,      4,  4,  8,  3) },
            { TextureFormat.Etc2RgbPtaUnorm,                  new FormatInfo(Format.Etc2RgbPtaUnorm,   4,  4,  8,  4) },
            { TextureFormat.Etc2RgbaUnorm,                    new FormatInfo(Format.Etc2RgbaUnorm,     4,  4,  16, 4) },
            { TextureFormat.Etc2RgbUnormSrgb,                 new FormatInfo(Format.Etc2RgbSrgb,       4,  4,  8,  3) },
            { TextureFormat.Etc2RgbPtaUnormSrgb,              new FormatInfo(Format.Etc2RgbPtaSrgb,    4,  4,  8,  4) },
            { TextureFormat.Etc2RgbaUnormSrgb,                new FormatInfo(Format.Etc2RgbaSrgb,      4,  4,  16, 4) },
            { TextureFormat.Astc2D4x4Unorm,                   new FormatInfo(Format.Astc4x4Unorm,      4,  4,  16, 4) },
            { TextureFormat.Astc2D5x4Unorm,                   new FormatInfo(Format.Astc5x4Unorm,      5,  4,  16, 4) },
            { TextureFormat.Astc2D5x5Unorm,                   new FormatInfo(Format.Astc5x5Unorm,      5,  5,  16, 4) },
            { TextureFormat.Astc2D6x5Unorm,                   new FormatInfo(Format.Astc6x5Unorm,      6,  5,  16, 4) },
            { TextureFormat.Astc2D6x6Unorm,                   new FormatInfo(Format.Astc6x6Unorm,      6,  6,  16, 4) },
            { TextureFormat.Astc2D8x5Unorm,                   new FormatInfo(Format.Astc8x5Unorm,      8,  5,  16, 4) },
            { TextureFormat.Astc2D8x6Unorm,                   new FormatInfo(Format.Astc8x6Unorm,      8,  6,  16, 4) },
            { TextureFormat.Astc2D8x8Unorm,                   new FormatInfo(Format.Astc8x8Unorm,      8,  8,  16, 4) },
            { TextureFormat.Astc2D10x5Unorm,                  new FormatInfo(Format.Astc10x5Unorm,     10, 5,  16, 4) },
            { TextureFormat.Astc2D10x6Unorm,                  new FormatInfo(Format.Astc10x6Unorm,     10, 6,  16, 4) },
            { TextureFormat.Astc2D10x8Unorm,                  new FormatInfo(Format.Astc10x8Unorm,     10, 8,  16, 4) },
            { TextureFormat.Astc2D10x10Unorm,                 new FormatInfo(Format.Astc10x10Unorm,    10, 10, 16, 4) },
            { TextureFormat.Astc2D12x10Unorm,                 new FormatInfo(Format.Astc12x10Unorm,    12, 10, 16, 4) },
            { TextureFormat.Astc2D12x12Unorm,                 new FormatInfo(Format.Astc12x12Unorm,    12, 12, 16, 4) },
            { TextureFormat.Astc2D4x4UnormSrgb,               new FormatInfo(Format.Astc4x4Srgb,       4,  4,  16, 4) },
            { TextureFormat.Astc2D5x4UnormSrgb,               new FormatInfo(Format.Astc5x4Srgb,       5,  4,  16, 4) },
            { TextureFormat.Astc2D5x5UnormSrgb,               new FormatInfo(Format.Astc5x5Srgb,       5,  5,  16, 4) },
            { TextureFormat.Astc2D6x5UnormSrgb,               new FormatInfo(Format.Astc6x5Srgb,       6,  5,  16, 4) },
            { TextureFormat.Astc2D6x6UnormSrgb,               new FormatInfo(Format.Astc6x6Srgb,       6,  6,  16, 4) },
            { TextureFormat.Astc2D8x5UnormSrgb,               new FormatInfo(Format.Astc8x5Srgb,       8,  5,  16, 4) },
            { TextureFormat.Astc2D8x6UnormSrgb,               new FormatInfo(Format.Astc8x6Srgb,       8,  6,  16, 4) },
            { TextureFormat.Astc2D8x8UnormSrgb,               new FormatInfo(Format.Astc8x8Srgb,       8,  8,  16, 4) },
            { TextureFormat.Astc2D10x5UnormSrgb,              new FormatInfo(Format.Astc10x5Srgb,      10, 5,  16, 4) },
            { TextureFormat.Astc2D10x6UnormSrgb,              new FormatInfo(Format.Astc10x6Srgb,      10, 6,  16, 4) },
            { TextureFormat.Astc2D10x8UnormSrgb,              new FormatInfo(Format.Astc10x8Srgb,      10, 8,  16, 4) },
            { TextureFormat.Astc2D10x10UnormSrgb,             new FormatInfo(Format.Astc10x10Srgb,     10, 10, 16, 4) },
            { TextureFormat.Astc2D12x10UnormSrgb,             new FormatInfo(Format.Astc12x10Srgb,     12, 10, 16, 4) },
            { TextureFormat.Astc2D12x12UnormSrgb,             new FormatInfo(Format.Astc12x12Srgb,     12, 12, 16, 4) },
            { TextureFormat.A5B5G5R1Unorm,                    new FormatInfo(Format.A1B5G5R5Unorm,     1,  1,  2,  4) },
        };

        private static readonly Dictionary<VertexAttributeFormat, Format> _attribFormats = new()
        {
            { VertexAttributeFormat.R8Unorm,             Format.R8Unorm             },
            { VertexAttributeFormat.R8Snorm,             Format.R8Snorm             },
            { VertexAttributeFormat.R8Uint,              Format.R8Uint              },
            { VertexAttributeFormat.R8Sint,              Format.R8Sint              },
            { VertexAttributeFormat.R16Float,            Format.R16Float            },
            { VertexAttributeFormat.R16Unorm,            Format.R16Unorm            },
            { VertexAttributeFormat.R16Snorm,            Format.R16Snorm            },
            { VertexAttributeFormat.R16Uint,             Format.R16Uint             },
            { VertexAttributeFormat.R16Sint,             Format.R16Sint             },
            { VertexAttributeFormat.R32Float,            Format.R32Float            },
            { VertexAttributeFormat.R32Uint,             Format.R32Uint             },
            { VertexAttributeFormat.R32Sint,             Format.R32Sint             },
            { VertexAttributeFormat.R8G8Unorm,           Format.R8G8Unorm           },
            { VertexAttributeFormat.R8G8Snorm,           Format.R8G8Snorm           },
            { VertexAttributeFormat.R8G8Uint,            Format.R8G8Uint            },
            { VertexAttributeFormat.R8G8Sint,            Format.R8G8Sint            },
            { VertexAttributeFormat.R16G16Float,         Format.R16G16Float         },
            { VertexAttributeFormat.R16G16Unorm,         Format.R16G16Unorm         },
            { VertexAttributeFormat.R16G16Snorm,         Format.R16G16Snorm         },
            { VertexAttributeFormat.R16G16Uint,          Format.R16G16Uint          },
            { VertexAttributeFormat.R16G16Sint,          Format.R16G16Sint          },
            { VertexAttributeFormat.R32G32Float,         Format.R32G32Float         },
            { VertexAttributeFormat.R32G32Uint,          Format.R32G32Uint          },
            { VertexAttributeFormat.R32G32Sint,          Format.R32G32Sint          },
            { VertexAttributeFormat.R8G8B8Unorm,         Format.R8G8B8Unorm         },
            { VertexAttributeFormat.R8G8B8Snorm,         Format.R8G8B8Snorm         },
            { VertexAttributeFormat.R8G8B8Uint,          Format.R8G8B8Uint          },
            { VertexAttributeFormat.R8G8B8Sint,          Format.R8G8B8Sint          },
            { VertexAttributeFormat.R16G16B16Float,      Format.R16G16B16Float      },
            { VertexAttributeFormat.R16G16B16Unorm,      Format.R16G16B16Unorm      },
            { VertexAttributeFormat.R16G16B16Snorm,      Format.R16G16B16Snorm      },
            { VertexAttributeFormat.R16G16B16Uint,       Format.R16G16B16Uint       },
            { VertexAttributeFormat.R16G16B16Sint,       Format.R16G16B16Sint       },
            { VertexAttributeFormat.R32G32B32Float,      Format.R32G32B32Float      },
            { VertexAttributeFormat.R32G32B32Uint,       Format.R32G32B32Uint       },
            { VertexAttributeFormat.R32G32B32Sint,       Format.R32G32B32Sint       },
            { VertexAttributeFormat.R8G8B8A8Unorm,       Format.R8G8B8A8Unorm       },
            { VertexAttributeFormat.R8G8B8A8Snorm,       Format.R8G8B8A8Snorm       },
            { VertexAttributeFormat.R8G8B8A8Uint,        Format.R8G8B8A8Uint        },
            { VertexAttributeFormat.R8G8B8A8Sint,        Format.R8G8B8A8Sint        },
            { VertexAttributeFormat.R16G16B16A16Float,   Format.R16G16B16A16Float   },
            { VertexAttributeFormat.R16G16B16A16Unorm,   Format.R16G16B16A16Unorm   },
            { VertexAttributeFormat.R16G16B16A16Snorm,   Format.R16G16B16A16Snorm   },
            { VertexAttributeFormat.R16G16B16A16Uint,    Format.R16G16B16A16Uint    },
            { VertexAttributeFormat.R16G16B16A16Sint,    Format.R16G16B16A16Sint    },
            { VertexAttributeFormat.R32G32B32A32Float,   Format.R32G32B32A32Float   },
            { VertexAttributeFormat.R32G32B32A32Uint,    Format.R32G32B32A32Uint    },
            { VertexAttributeFormat.R32G32B32A32Sint,    Format.R32G32B32A32Sint    },
            { VertexAttributeFormat.A2B10G10R10Unorm,    Format.R10G10B10A2Unorm    },
            { VertexAttributeFormat.A2B10G10R10Uint,     Format.R10G10B10A2Uint     },
            { VertexAttributeFormat.B10G11R11Float,      Format.R11G11B10Float      },
            { VertexAttributeFormat.R8Uscaled,           Format.R8Uscaled           },
            { VertexAttributeFormat.R8Sscaled,           Format.R8Sscaled           },
            { VertexAttributeFormat.R16Uscaled,          Format.R16Uscaled          },
            { VertexAttributeFormat.R16Sscaled,          Format.R16Sscaled          },
            { VertexAttributeFormat.R32Uscaled,          Format.R32Uscaled          },
            { VertexAttributeFormat.R32Sscaled,          Format.R32Sscaled          },
            { VertexAttributeFormat.R8G8Uscaled,         Format.R8G8Uscaled         },
            { VertexAttributeFormat.R8G8Sscaled,         Format.R8G8Sscaled         },
            { VertexAttributeFormat.R16G16Uscaled,       Format.R16G16Uscaled       },
            { VertexAttributeFormat.R16G16Sscaled,       Format.R16G16Sscaled       },
            { VertexAttributeFormat.R32G32Uscaled,       Format.R32G32Uscaled       },
            { VertexAttributeFormat.R32G32Sscaled,       Format.R32G32Sscaled       },
            { VertexAttributeFormat.R8G8B8Uscaled,       Format.R8G8B8Uscaled       },
            { VertexAttributeFormat.R8G8B8Sscaled,       Format.R8G8B8Sscaled       },
            { VertexAttributeFormat.R16G16B16Uscaled,    Format.R16G16B16Uscaled    },
            { VertexAttributeFormat.R16G16B16Sscaled,    Format.R16G16B16Sscaled    },
            { VertexAttributeFormat.R32G32B32Uscaled,    Format.R32G32B32Uscaled    },
            { VertexAttributeFormat.R32G32B32Sscaled,    Format.R32G32B32Sscaled    },
            { VertexAttributeFormat.R8G8B8A8Uscaled,     Format.R8G8B8A8Uscaled     },
            { VertexAttributeFormat.R8G8B8A8Sscaled,     Format.R8G8B8A8Sscaled     },
            { VertexAttributeFormat.R16G16B16A16Uscaled, Format.R16G16B16A16Uscaled },
            { VertexAttributeFormat.R16G16B16A16Sscaled, Format.R16G16B16A16Sscaled },
            { VertexAttributeFormat.R32G32B32A32Uscaled, Format.R32G32B32A32Uscaled },
            { VertexAttributeFormat.R32G32B32A32Sscaled, Format.R32G32B32A32Sscaled },
            { VertexAttributeFormat.A2B10G10R10Snorm,    Format.R10G10B10A2Snorm    },
            { VertexAttributeFormat.A2B10G10R10Sint,     Format.R10G10B10A2Sint     },
            { VertexAttributeFormat.A2B10G10R10Uscaled,  Format.R10G10B10A2Uscaled  },
            { VertexAttributeFormat.A2B10G10R10Sscaled,  Format.R10G10B10A2Sscaled  },
        };
#pragma warning restore IDE0055

        // Note: Some of those formats have been changed and requires conversion on the shader,
        // as GPUs don't support them when used as buffer texture format.
        private static readonly Dictionary<VertexAttributeFormat, (Format, int)> _singleComponentAttribFormats = new()
        {
            { VertexAttributeFormat.R8Unorm,             (Format.R8Unorm, 1)          },
            { VertexAttributeFormat.R8Snorm,             (Format.R8Snorm, 1)          },
            { VertexAttributeFormat.R8Uint,              (Format.R8Uint, 1)           },
            { VertexAttributeFormat.R8Sint,              (Format.R8Sint, 1)           },
            { VertexAttributeFormat.R16Float,            (Format.R16Float, 1)         },
            { VertexAttributeFormat.R16Unorm,            (Format.R16Unorm, 1)         },
            { VertexAttributeFormat.R16Snorm,            (Format.R16Snorm, 1)         },
            { VertexAttributeFormat.R16Uint,             (Format.R16Uint, 1)          },
            { VertexAttributeFormat.R16Sint,             (Format.R16Sint, 1)          },
            { VertexAttributeFormat.R32Float,            (Format.R32Float, 1)         },
            { VertexAttributeFormat.R32Uint,             (Format.R32Uint, 1)          },
            { VertexAttributeFormat.R32Sint,             (Format.R32Sint, 1)          },
            { VertexAttributeFormat.R8G8Unorm,           (Format.R8Unorm, 2)          },
            { VertexAttributeFormat.R8G8Snorm,           (Format.R8Snorm, 2)          },
            { VertexAttributeFormat.R8G8Uint,            (Format.R8Uint, 2)           },
            { VertexAttributeFormat.R8G8Sint,            (Format.R8Sint, 2)           },
            { VertexAttributeFormat.R16G16Float,         (Format.R16Float, 2)         },
            { VertexAttributeFormat.R16G16Unorm,         (Format.R16Unorm, 2)         },
            { VertexAttributeFormat.R16G16Snorm,         (Format.R16Snorm, 2)         },
            { VertexAttributeFormat.R16G16Uint,          (Format.R16Uint, 2)          },
            { VertexAttributeFormat.R16G16Sint,          (Format.R16Sint, 2)          },
            { VertexAttributeFormat.R32G32Float,         (Format.R32Float, 2)         },
            { VertexAttributeFormat.R32G32Uint,          (Format.R32Uint, 2)          },
            { VertexAttributeFormat.R32G32Sint,          (Format.R32Sint, 2)          },
            { VertexAttributeFormat.R8G8B8Unorm,         (Format.R8Unorm, 3)          },
            { VertexAttributeFormat.R8G8B8Snorm,         (Format.R8Snorm, 3)          },
            { VertexAttributeFormat.R8G8B8Uint,          (Format.R8Uint, 3)           },
            { VertexAttributeFormat.R8G8B8Sint,          (Format.R8Sint, 3)           },
            { VertexAttributeFormat.R16G16B16Float,      (Format.R16Float, 3)         },
            { VertexAttributeFormat.R16G16B16Unorm,      (Format.R16Unorm, 3)         },
            { VertexAttributeFormat.R16G16B16Snorm,      (Format.R16Snorm, 3)         },
            { VertexAttributeFormat.R16G16B16Uint,       (Format.R16Uint, 3)          },
            { VertexAttributeFormat.R16G16B16Sint,       (Format.R16Sint, 3)          },
            { VertexAttributeFormat.R32G32B32Float,      (Format.R32Float, 3)         },
            { VertexAttributeFormat.R32G32B32Uint,       (Format.R32Uint, 3)          },
            { VertexAttributeFormat.R32G32B32Sint,       (Format.R32Sint, 3)          },
            { VertexAttributeFormat.R8G8B8A8Unorm,       (Format.R8Unorm, 4)          },
            { VertexAttributeFormat.R8G8B8A8Snorm,       (Format.R8Snorm, 4)          },
            { VertexAttributeFormat.R8G8B8A8Uint,        (Format.R8Uint, 4)           },
            { VertexAttributeFormat.R8G8B8A8Sint,        (Format.R8Sint, 4)           },
            { VertexAttributeFormat.R16G16B16A16Float,   (Format.R16Float, 4)         },
            { VertexAttributeFormat.R16G16B16A16Unorm,   (Format.R16Unorm, 4)         },
            { VertexAttributeFormat.R16G16B16A16Snorm,   (Format.R16Snorm, 4)         },
            { VertexAttributeFormat.R16G16B16A16Uint,    (Format.R16Uint, 4)          },
            { VertexAttributeFormat.R16G16B16A16Sint,    (Format.R16Sint, 4)          },
            { VertexAttributeFormat.R32G32B32A32Float,   (Format.R32Float, 4)         },
            { VertexAttributeFormat.R32G32B32A32Uint,    (Format.R32Uint, 4)          },
            { VertexAttributeFormat.R32G32B32A32Sint,    (Format.R32Sint, 4)          },
            { VertexAttributeFormat.A2B10G10R10Unorm,    (Format.R10G10B10A2Unorm, 4) },
            { VertexAttributeFormat.A2B10G10R10Uint,     (Format.R10G10B10A2Uint, 4)  },
            { VertexAttributeFormat.B10G11R11Float,      (Format.R11G11B10Float, 3)   },
            { VertexAttributeFormat.R8Uscaled,           (Format.R8Uint, 1)           }, // Uscaled -> Uint
            { VertexAttributeFormat.R8Sscaled,           (Format.R8Sint, 1)           }, // Sscaled -> Sint
            { VertexAttributeFormat.R16Uscaled,          (Format.R16Uint, 1)          }, // Uscaled -> Uint
            { VertexAttributeFormat.R16Sscaled,          (Format.R16Sint, 1)          }, // Sscaled -> Sint
            { VertexAttributeFormat.R32Uscaled,          (Format.R32Uint, 1)          }, // Uscaled -> Uint
            { VertexAttributeFormat.R32Sscaled,          (Format.R32Sint, 1)          }, // Sscaled -> Sint
            { VertexAttributeFormat.R8G8Uscaled,         (Format.R8Uint, 2)           }, // Uscaled -> Uint
            { VertexAttributeFormat.R8G8Sscaled,         (Format.R8Sint, 2)           }, // Sscaled -> Sint
            { VertexAttributeFormat.R16G16Uscaled,       (Format.R16Uint, 2)          }, // Uscaled -> Uint
            { VertexAttributeFormat.R16G16Sscaled,       (Format.R16Sint, 2)          }, // Sscaled -> Sint
            { VertexAttributeFormat.R32G32Uscaled,       (Format.R32Uint, 2)          }, // Uscaled -> Uint
            { VertexAttributeFormat.R32G32Sscaled,       (Format.R32Sint, 2)          }, // Sscaled -> Sint
            { VertexAttributeFormat.R8G8B8Uscaled,       (Format.R8Uint, 3)           }, // Uscaled -> Uint
            { VertexAttributeFormat.R8G8B8Sscaled,       (Format.R8Sint, 3)           }, // Sscaled -> Sint
            { VertexAttributeFormat.R16G16B16Uscaled,    (Format.R16Uint, 3)          }, // Uscaled -> Uint
            { VertexAttributeFormat.R16G16B16Sscaled,    (Format.R16Sint, 3)          }, // Sscaled -> Sint
            { VertexAttributeFormat.R32G32B32Uscaled,    (Format.R32Uint, 3)          }, // Uscaled -> Uint
            { VertexAttributeFormat.R32G32B32Sscaled,    (Format.R32Sint , 3)         }, // Sscaled -> Sint
            { VertexAttributeFormat.R8G8B8A8Uscaled,     (Format.R8Uint, 4)           }, // Uscaled -> Uint
            { VertexAttributeFormat.R8G8B8A8Sscaled,     (Format.R8Sint, 4)           }, // Sscaled -> Sint
            { VertexAttributeFormat.R16G16B16A16Uscaled, (Format.R16Uint, 4)          }, // Uscaled -> Uint
            { VertexAttributeFormat.R16G16B16A16Sscaled, (Format.R16Sint, 4)          }, // Sscaled -> Sint
            { VertexAttributeFormat.R32G32B32A32Uscaled, (Format.R32Uint, 4)          }, // Uscaled -> Uint
            { VertexAttributeFormat.R32G32B32A32Sscaled, (Format.R32Sint, 4)          }, // Sscaled -> Sint
            { VertexAttributeFormat.A2B10G10R10Snorm,    (Format.R10G10B10A2Uint, 4)  }, // Snorm -> Uint
            { VertexAttributeFormat.A2B10G10R10Sint,     (Format.R10G10B10A2Uint, 4)  }, // Sint -> Uint
            { VertexAttributeFormat.A2B10G10R10Uscaled,  (Format.R10G10B10A2Uint, 4)  }, // Uscaled -> Uint
            { VertexAttributeFormat.A2B10G10R10Sscaled,  (Format.R10G10B10A2Sint, 4)  }  // Sscaled -> Sint
        };

        /// <summary>
        /// Try getting the texture format from an encoded format integer from the Maxwell texture descriptor.
        /// </summary>
        /// <param name="encoded">The encoded format integer from the texture descriptor</param>
        /// <param name="isSrgb">Indicates if the format is a sRGB format</param>
        /// <param name="format">The output texture format</param>
        /// <returns>True if the format is valid, false otherwise</returns>
        public static bool TryGetTextureFormat(uint encoded, bool isSrgb, out FormatInfo format)
        {
            bool isPacked = (encoded & 0x80000000u) != 0;
            if (isPacked)
            {
                encoded &= ~0x80000000u;
            }

            encoded |= isSrgb ? 1u << 19 : 0u;

            bool found = _textureFormats.TryGetValue((TextureFormat)encoded, out format);

            if (found && isPacked && !format.Format.IsDepthOrStencil())
            {
                // If the packed flag is set, then the components of the pixel are tightly packed into the
                // GPU registers on the shader.
                // We can get the same behaviour by aliasing the texture as a format with the same amount of
                // bytes per pixel, but only a single or the lowest possible number of components.

                format = format.BytesPerPixel switch
                {
                    1 => new FormatInfo(Format.R8Unorm, 1, 1, 1, 1),
                    2 => new FormatInfo(Format.R16Unorm, 1, 1, 2, 1),
                    4 => new FormatInfo(Format.R32Uint, 1, 1, 4, 1),
                    8 => new FormatInfo(Format.R32G32Uint, 1, 1, 8, 2),
                    16 => new FormatInfo(Format.R32G32B32A32Uint, 1, 1, 16, 4),
                    _ => format,
                };
            }

            return found;
        }

        /// <summary>
        /// Try getting the vertex attribute format from an encoded format integer from Maxwell attribute registers.
        /// </summary>
        /// <param name="encoded">The encoded format integer from the attribute registers</param>
        /// <param name="format">The output vertex attribute format</param>
        /// <returns>True if the format is valid, false otherwise</returns>
        public static bool TryGetAttribFormat(uint encoded, out Format format)
        {
            return _attribFormats.TryGetValue((VertexAttributeFormat)encoded, out format);
        }

        /// <summary>
        /// Try getting a single component vertex attribute format from an encoded format integer from Maxwell attribute registers.
        /// </summary>
        /// <param name="encoded">The encoded format integer from the attribute registers</param>
        /// <param name="format">The output single component vertex attribute format</param>
        /// <param name="componentsCount">Number of components that the format has</param>
        /// <returns>True if the format is valid, false otherwise</returns>
        public static bool TryGetSingleComponentAttribFormat(uint encoded, out Format format, out int componentsCount)
        {
            bool result = _singleComponentAttribFormats.TryGetValue((VertexAttributeFormat)encoded, out (Format, int) tuple);

            format = tuple.Item1;
            componentsCount = tuple.Item2;

            return result;
        }
    }
}
