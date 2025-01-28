using OpenTK.Graphics.OpenGL;
using Ryujinx.Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Graphics.OpenGL
{
    class Sync : IDisposable
    {
        private class SyncHandle
        {
            public ulong ID;
            public nint Handle;
        }

        private ulong _firstHandle = 0;
        private static ClientWaitSyncFlags SyncFlags => HwCapabilities.RequiresSyncFlush ? ClientWaitSyncFlags.None : ClientWaitSyncFlags.SyncFlushCommandsBit;

        private readonly List<SyncHandle> _handles = [];

        public void Create(ulong id)
        {
            SyncHandle handle = new()
            {
                ID = id,
                Handle = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags.None),
            };


            if (HwCapabilities.RequiresSyncFlush)
            {
                // Force commands to flush up to the syncpoint.
                GL.ClientWaitSync(handle.Handle, ClientWaitSyncFlags.SyncFlushCommandsBit, 0);
            }

            lock (_handles)
            {
                _handles.Add(handle);
            }
        }

        public ulong GetCurrent()
        {
            lock (_handles)
            {
                ulong lastHandle = _firstHandle;

                foreach (SyncHandle handle in _handles)
                {
                    lock (handle)
                    {
                        if (handle.Handle == nint.Zero)
                        {
                            continue;
                        }

                        if (handle.ID > lastHandle)
                        {
                            WaitSyncStatus syncResult = GL.ClientWaitSync(handle.Handle, SyncFlags, 0);

                            if (syncResult == WaitSyncStatus.AlreadySignaled)
                            {
                                lastHandle = handle.ID;
                            }
                        }
                    }
                }

                return lastHandle;
            }
        }

        public void Wait(ulong id)
        {
            SyncHandle result = null;

            lock (_handles)
            {
                if ((long)(_firstHandle - id) > 0)
                {
                    return; // The handle has already been signalled or deleted.
                }

                foreach (SyncHandle handle in _handles)
                {
                    if (handle.ID == id)
                    {
                        result = handle;
                        break;
                    }
                }
            }

            if (result != null)
            {
                lock (result)
                {
                    if (result.Handle == nint.Zero)
                    {
                        return;
                    }

                    WaitSyncStatus syncResult = GL.ClientWaitSync(result.Handle, SyncFlags, 1000000000);

                    if (syncResult == WaitSyncStatus.TimeoutExpired)
                    {
                        Logger.Error?.PrintMsg(LogClass.Gpu, $"GL Sync Object {result.ID} failed to signal within 1000ms. Continuing...");
                    }
                }
            }
        }

        public void Cleanup()
        {
            // Iterate through handles and remove any that have already been signalled.

            while (true)
            {
                SyncHandle first = null;
                lock (_handles)
                {
                    first = _handles.FirstOrDefault();
                }

                if (first == null)
                {
                    break;
                }

                WaitSyncStatus syncResult = GL.ClientWaitSync(first.Handle, SyncFlags, 0);

                if (syncResult == WaitSyncStatus.AlreadySignaled)
                {
                    // Delete the sync object.
                    lock (_handles)
                    {
                        lock (first)
                        {
                            _firstHandle = first.ID + 1;
                            _handles.RemoveAt(0);
                            GL.DeleteSync(first.Handle);
                            first.Handle = nint.Zero;
                        }
                    }
                }
                else
                {
                    // This sync handle and any following have not been reached yet.
                    break;
                }
            }
        }

        public void Dispose()
        {
            lock (_handles)
            {
                foreach (SyncHandle handle in _handles)
                {
                    lock (handle)
                    {
                        GL.DeleteSync(handle.Handle);
                        handle.Handle = nint.Zero;
                    }
                }

                _handles.Clear();
            }
        }
    }
}
