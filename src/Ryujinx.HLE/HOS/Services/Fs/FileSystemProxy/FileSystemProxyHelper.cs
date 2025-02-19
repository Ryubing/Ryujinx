using LibHac;
using LibHac.Common;
using LibHac.Common.Keys;
using LibHac.Fs;
using LibHac.FsSrv.Impl;
using LibHac.FsSrv.Sf;
using LibHac.FsSystem;
using LibHac.Spl;
using LibHac.Tools.Es;
using LibHac.Tools.Fs;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Path = System.IO.Path;

namespace Ryujinx.HLE.HOS.Services.Fs.FileSystemProxy
{
    static class FileSystemProxyHelper
    {
        public static ResultCode OpenNsp(ServiceCtx context, string pfsPath, out IFileSystem openedFileSystem)
        {
            openedFileSystem = null;

            try
            {
                LocalStorage storage = new(pfsPath, FileAccess.Read, FileMode.Open);
                PartitionFileSystem pfs = new();
                using SharedRef<LibHac.Fs.Fsa.IFileSystem> nsp = new(pfs);
                pfs.Initialize(storage).ThrowIfFailure();

                ImportTitleKeysFromNsp(nsp.Get, context.Device.System.KeySet);

                using SharedRef<LibHac.FsSrv.Sf.IFileSystem> adapter = FileSystemInterfaceAdapter.CreateShared(ref nsp.Ref, true);

                openedFileSystem = new IFileSystem(ref adapter.Ref);
            }
            catch (HorizonResultException ex)
            {
                return (ResultCode)ex.ResultValue.Value;
            }

            return ResultCode.Success;
        }

        public static ResultCode OpenNcaFs(ServiceCtx context, string ncaPath, LibHac.Fs.IStorage ncaStorage, out IFileSystem openedFileSystem)
        {
            openedFileSystem = null;

            try
            {
                Nca nca = new(context.Device.System.KeySet, ncaStorage);

                if (!nca.SectionExists(NcaSectionType.Data))
                {
                    return ResultCode.PartitionNotFound;
                }

                LibHac.Fs.Fsa.IFileSystem fileSystem = nca.OpenFileSystem(NcaSectionType.Data, context.Device.System.FsIntegrityCheckLevel);
                using SharedRef<LibHac.Fs.Fsa.IFileSystem> sharedFs = new(fileSystem);

                using SharedRef<LibHac.FsSrv.Sf.IFileSystem> adapter = FileSystemInterfaceAdapter.CreateShared(ref sharedFs.Ref, true);

                openedFileSystem = new IFileSystem(ref adapter.Ref);
            }
            catch (HorizonResultException ex)
            {
                return (ResultCode)ex.ResultValue.Value;
            }

            return ResultCode.Success;
        }

        public static ResultCode OpenFileSystemFromInternalFile(ServiceCtx context, string fullPath, out IFileSystem openedFileSystem)
        {
            openedFileSystem = null;

            DirectoryInfo archivePath = new DirectoryInfo(fullPath).Parent;

            while (string.IsNullOrWhiteSpace(archivePath.Extension))
            {
                archivePath = archivePath.Parent;
            }

            if (archivePath.Extension == ".nsp" && File.Exists(archivePath.FullName))
            {
                FileStream pfsFile = new(
                    archivePath.FullName.TrimEnd(Path.DirectorySeparatorChar),
                    FileMode.Open,
                    FileAccess.Read);

                try
                {
                    PartitionFileSystem nsp = new();
                    nsp.Initialize(pfsFile.AsStorage()).ThrowIfFailure();

                    ImportTitleKeysFromNsp(nsp, context.Device.System.KeySet);

                    string filename = fullPath.Replace(archivePath.FullName, string.Empty).TrimStart('\\');

                    using UniqueRef<LibHac.Fs.Fsa.IFile> ncaFile = new();

                    Result result = nsp.OpenFile(ref ncaFile.Ref, filename.ToU8Span(), OpenMode.Read);
                    if (result.IsFailure())
                    {
                        return (ResultCode)result.Value;
                    }

                    return OpenNcaFs(context, fullPath, ncaFile.Release().AsStorage(), out openedFileSystem);
                }
                catch (HorizonResultException ex)
                {
                    return (ResultCode)ex.ResultValue.Value;
                }
            }

            return ResultCode.PathDoesNotExist;
        }

        public static void ImportTitleKeysFromNsp(LibHac.Fs.Fsa.IFileSystem nsp, KeySet keySet)
        {
            foreach (DirectoryEntryEx ticketEntry in nsp.EnumerateEntries("/", "*.tik"))
            {
                using UniqueRef<LibHac.Fs.Fsa.IFile> ticketFile = new();

                Result result = nsp.OpenFile(ref ticketFile.Ref, ticketEntry.FullPath.ToU8Span(), OpenMode.Read);

                if (result.IsSuccess())
                {
                    Ticket ticket = new(ticketFile.Get.AsStream());
                    byte[] titleKey = ticket.GetTitleKey(keySet);

                    if (titleKey != null)
                    {
                        keySet.ExternalKeySet.Add(new RightsId(ticket.RightsId), new AccessKey(titleKey));
                    }
                }
            }
        }

        public static ref readonly FspPath GetFspPath(ServiceCtx context, int index = 0)
        {
            ulong position = context.Request.PtrBuff[index].Position;
            ulong size = context.Request.PtrBuff[index].Size;

            ReadOnlySpan<byte> buffer = context.Memory.GetSpan(position, (int)size);
            ReadOnlySpan<FspPath> fspBuffer = MemoryMarshal.Cast<byte, FspPath>(buffer);

            return ref fspBuffer[0];
        }

        public static ref readonly LibHac.FsSrv.Sf.Path GetSfPath(ServiceCtx context, int index = 0)
        {
            ulong position = context.Request.PtrBuff[index].Position;
            ulong size = context.Request.PtrBuff[index].Size;

            ReadOnlySpan<byte> buffer = context.Memory.GetSpan(position, (int)size);
            ReadOnlySpan<LibHac.FsSrv.Sf.Path> pathBuffer = MemoryMarshal.Cast<byte, LibHac.FsSrv.Sf.Path>(buffer);

            return ref pathBuffer[0];
        }
    }
}
