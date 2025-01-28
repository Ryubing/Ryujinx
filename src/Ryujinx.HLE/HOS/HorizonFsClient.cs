using LibHac.Common;
using LibHac.Fs.Fsa;
using LibHac.FsSystem;
using LibHac.Ncm;
using LibHac.Tools.FsSystem.NcaUtils;
using Ryujinx.HLE.FileSystem;
using Ryujinx.Horizon;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Fs;
using System;
using System.Collections.Concurrent;
using System.IO;

namespace Ryujinx.HLE.HOS
{
    class HorizonFsClient : IFsClient
    {
        private readonly Horizon _system;
        private readonly LibHac.Fs.FileSystemClient _fsClient;
        private readonly ConcurrentDictionary<string, LocalStorage> _mountedStorages;

        public HorizonFsClient(Horizon system)
        {
            _system = system;
            _fsClient = _system.LibHacHorizonManager.FsClient.Fs;
            _mountedStorages = new();
        }

        public void CloseFile(FileHandle handle)
        {
            _fsClient.CloseFile((LibHac.Fs.FileHandle)handle.Value);
        }

        public Result GetFileSize(out long size, FileHandle handle)
        {
            return _fsClient.GetFileSize(out size, (LibHac.Fs.FileHandle)handle.Value).ToHorizonResult();
        }

        public Result MountSystemData(string mountName, ulong dataId)
        {
            string contentPath = _system.ContentManager.GetInstalledContentPath(dataId, StorageId.BuiltInSystem, NcaContentType.PublicData);
            string installPath = VirtualFileSystem.SwitchPathToSystemPath(contentPath);

            if (!string.IsNullOrWhiteSpace(installPath))
            {
                string ncaPath = installPath;

                if (File.Exists(ncaPath))
                {
                    LocalStorage ncaStorage = null;

                    try
                    {
                        ncaStorage = new LocalStorage(ncaPath, FileAccess.Read, FileMode.Open);

                        Nca nca = new(_system.KeySet, ncaStorage);

                        using IFileSystem ncaFileSystem = nca.OpenFileSystem(NcaSectionType.Data, _system.FsIntegrityCheckLevel);
                        using UniqueRef<IFileSystem> ncaFsRef = new(ncaFileSystem);

                        Result result = _fsClient.Register(mountName.ToU8Span(), ref ncaFsRef.Ref).ToHorizonResult();
                        if (result.IsFailure)
                        {
                            ncaStorage.Dispose();
                        }
                        else
                        {
                            _mountedStorages.TryAdd(mountName, ncaStorage);
                        }

                        return result;
                    }
                    catch (HorizonResultException ex)
                    {
                        ncaStorage?.Dispose();

                        return ex.ResultValue.ToHorizonResult();
                    }
                }
            }

            // TODO: Return correct result here, this is likely wrong.

            return LibHac.Fs.ResultFs.TargetNotFound.Handle().ToHorizonResult();
        }

        public Result OpenFile(out FileHandle handle, string path, OpenMode openMode)
        {
            LibHac.Result result = _fsClient.OpenFile(out LibHac.Fs.FileHandle libhacHandle, path.ToU8Span(), (LibHac.Fs.OpenMode)openMode);
            handle = new(libhacHandle);

            return result.ToHorizonResult();
        }

        public Result QueryMountSystemDataCacheSize(out long size, ulong dataId)
        {
            // TODO.

            size = 0;

            return Result.Success;
        }

        public Result ReadFile(FileHandle handle, long offset, Span<byte> destination)
        {
            return _fsClient.ReadFile((LibHac.Fs.FileHandle)handle.Value, offset, destination).ToHorizonResult();
        }

        public void Unmount(string mountName)
        {
            if (_mountedStorages.TryRemove(mountName, out LocalStorage ncaStorage))
            {
                ncaStorage.Dispose();
            }

            _fsClient.Unmount(mountName.ToU8Span());
        }
    }
}
