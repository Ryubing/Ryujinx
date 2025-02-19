namespace Ryujinx.Graphics.Nvdec.Vp9.Types
{
    internal enum MvClassType
    {
        Class0, /* (0, 2]     integer pel */
        Class1, /* (2, 4]     integer pel */
        Class2, /* (4, 8]     integer pel */
        Class3, /* (8, 16]    integer pel */
        Class4, /* (16, 32]   integer pel */
        Class5, /* (32, 64]   integer pel */
        Class6, /* (64, 128]  integer pel */
        Class7, /* (128, 256] integer pel */
        Class8, /* (256, 512] integer pel */
        Class9, /* (512, 1024] integer pel */
        Class10 /* (1024,2048] integer pel */
    }
}