using LibHac;
using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.FsSystem;
using LibHac.Ncm;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.Exceptions;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.HOS.Services.Ssl.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Ryujinx.HLE.HOS.Services.Ssl
{
    class BuiltInCertificateManager
    {
        private const long CertStoreTitleId = 0x0100000000000800;

        private const string CertStoreTitleMissingErrorMessage = "CertStore system title not found! SSL CA retrieving will not work, provide the system archive to fix this error.";

        private static BuiltInCertificateManager _instance;

        public static BuiltInCertificateManager Instance
        {
            get
            {
                _instance ??= new BuiltInCertificateManager();

                return _instance;
            }
        }

        private VirtualFileSystem _virtualFileSystem;
        private IntegrityCheckLevel _fsIntegrityCheckLevel;
        private ContentManager _contentManager;
        private bool _initialized;
        private Dictionary<CaCertificateId, CertStoreEntry> _certificates;

        private readonly Lock _lock = new();

        private struct CertStoreFileHeader
        {
            private const uint ValidMagic = 0x546C7373;

#pragma warning disable CS0649 // Field is never assigned to
            public uint Magic;
            public uint EntriesCount;
#pragma warning restore CS0649

            public readonly bool IsValid()
            {
                return Magic == ValidMagic;
            }
        }

        private struct CertStoreFileEntry
        {
#pragma warning disable CS0649 // Field is never assigned to
            public CaCertificateId Id;
            public TrustedCertStatus Status;
            public uint DataSize;
            public uint DataOffset;
#pragma warning restore CS0649
        }

        public class CertStoreEntry
        {
            public CaCertificateId Id;
            public TrustedCertStatus Status;
            public byte[] Data;
        }

        public string GetCertStoreTitleContentPath()
        {
            return _contentManager.GetInstalledContentPath(CertStoreTitleId, StorageId.BuiltInSystem, NcaContentType.Data);
        }

        public bool HasCertStoreTitle()
        {
            return !string.IsNullOrEmpty(GetCertStoreTitleContentPath());
        }

        private CertStoreEntry ReadCertStoreEntry(ReadOnlySpan<byte> buffer, CertStoreFileEntry entry)
        {
            string customCertificatePath = System.IO.Path.Join(AppDataManager.BaseDirPath, "system", "ssl", $"{entry.Id}.der");

            byte[] data;

            if (File.Exists(customCertificatePath))
            {
                data = File.ReadAllBytes(customCertificatePath);
            }
            else
            {
                data = buffer.Slice((int)entry.DataOffset, (int)entry.DataSize).ToArray();
            }

            return new CertStoreEntry
            {
                Id = entry.Id,
                Status = entry.Status,
                Data = data,
            };
        }

        public void Initialize(Switch device)
        {
            lock (_lock)
            {
                _certificates = new Dictionary<CaCertificateId, CertStoreEntry>();
                _initialized = false;
                _contentManager = device.System.ContentManager;
                _virtualFileSystem = device.FileSystem;
                _fsIntegrityCheckLevel = device.System.FsIntegrityCheckLevel;

                if (HasCertStoreTitle())
                {
                    using LocalStorage ncaFile = new(VirtualFileSystem.SwitchPathToSystemPath(GetCertStoreTitleContentPath()), FileAccess.Read, FileMode.Open);

                    Nca nca = new(_virtualFileSystem.KeySet, ncaFile);

                    IFileSystem romfs = nca.OpenFileSystem(NcaSectionType.Data, _fsIntegrityCheckLevel);

                    using var trustedCertsFileRef = new UniqueRef<IFile>();

                    Result result = romfs.OpenFile(ref trustedCertsFileRef.Ref, "/ssl_TrustedCerts.bdf".ToU8Span(), OpenMode.Read);

                    if (!result.IsSuccess())
                    {
                        // [1.0.0 - 2.3.0]
                        if (ResultFs.PathNotFound.Includes(result))
                        {
                            result = romfs.OpenFile(ref trustedCertsFileRef.Ref, "/ssl_TrustedCerts.tcf".ToU8Span(), OpenMode.Read);
                        }

                        if (result.IsFailure())
                        {
                            Logger.Error?.Print(LogClass.ServiceSsl, CertStoreTitleMissingErrorMessage);

                            return;
                        }
                    }

                    using IFile trustedCertsFile = trustedCertsFileRef.Release();

                    trustedCertsFile.GetSize(out long fileSize).ThrowIfFailure();

                    Span<byte> trustedCertsRaw = new byte[fileSize];

                    trustedCertsFile.Read(out _, 0, trustedCertsRaw).ThrowIfFailure();

                    CertStoreFileHeader header = MemoryMarshal.Read<CertStoreFileHeader>(trustedCertsRaw);

                    if (!header.IsValid())
                    {
                        Logger.Error?.Print(LogClass.ServiceSsl, "Invalid CertStore data found, skipping!");

                        return;
                    }

                    ReadOnlySpan<byte> trustedCertsData = trustedCertsRaw[Unsafe.SizeOf<CertStoreFileHeader>()..];
                    ReadOnlySpan<CertStoreFileEntry> trustedCertsEntries = MemoryMarshal.Cast<byte, CertStoreFileEntry>(trustedCertsData)[..(int)header.EntriesCount];

                    foreach (CertStoreFileEntry entry in trustedCertsEntries)
                    {
                        _certificates.Add(entry.Id, ReadCertStoreEntry(trustedCertsData, entry));
                    }

                    _initialized = true;
                }
            }
        }

        public bool TryGetCertificates(
            ReadOnlySpan<CaCertificateId> ids,
            out CertStoreEntry[] entries,
            out bool hasAllCertificates,
            out int requiredSize)
        {
            lock (_lock)
            {
                if (!_initialized)
                {
                    throw new InvalidSystemResourceException(CertStoreTitleMissingErrorMessage);
                }

                requiredSize = 0;
                hasAllCertificates = false;

                foreach (CaCertificateId id in ids)
                {
                    if (id == CaCertificateId.All)
                    {
                        hasAllCertificates = true;

                        break;
                    }
                }

                if (hasAllCertificates)
                {
                    entries = new CertStoreEntry[_certificates.Count];
                    requiredSize = (_certificates.Count + 1) * Unsafe.SizeOf<BuiltInCertificateInfo>();

                    int i = 0;

                    foreach (CertStoreEntry entry in _certificates.Values)
                    {
                        entries[i++] = entry;
                        requiredSize += (entry.Data.Length + 3) & ~3;
                    }

                    return true;
                }
                else
                {
                    entries = new CertStoreEntry[ids.Length];
                    requiredSize = ids.Length * Unsafe.SizeOf<BuiltInCertificateInfo>();

                    for (int i = 0; i < ids.Length; i++)
                    {
                        if (!_certificates.TryGetValue(ids[i], out CertStoreEntry entry))
                        {
                            return false;
                        }

                        entries[i] = entry;
                        requiredSize += (entry.Data.Length + 3) & ~3;
                    }

                    return true;
                }
            }
        }
    }
}
