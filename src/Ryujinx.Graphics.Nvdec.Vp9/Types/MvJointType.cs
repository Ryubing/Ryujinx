namespace Ryujinx.Graphics.Nvdec.Vp9.Types
{
    internal enum MvJointType
    {
        Zero, /* Zero vector */
        Hnzvz, /* Vert zero, hor nonzero */
        Hzvnz, /* Hor zero, vert nonzero */
        Hnzvnz /* Both components nonzero */
    }
}