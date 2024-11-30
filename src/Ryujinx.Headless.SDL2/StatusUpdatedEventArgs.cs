using System;

namespace Ryujinx.Headless.SDL2
{
    class StatusUpdatedEventArgs(
        string vSyncMode,
        string dockedMode,
        string aspectRatio,
        string gameStatus,
        string fifoStatus,
        string gpuName)
        : EventArgs
    {
        public string VSyncMode = vSyncMode;
        public string DockedMode = dockedMode;
        public string AspectRatio = aspectRatio;
        public string GameStatus = gameStatus;
        public string FifoStatus = fifoStatus;
        public string GpuName = gpuName;
    }
}
