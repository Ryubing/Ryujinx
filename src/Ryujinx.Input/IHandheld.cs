using System;
using System.Numerics;

namespace Ryujinx.Input
{
    public interface IHandheld : IDisposable
    {
        Vector3 GetMotionData(MotionInputId gyroscope);
    }
}
