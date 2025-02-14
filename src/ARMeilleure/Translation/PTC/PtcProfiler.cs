using ARMeilleure.State;
using Humanizer;
using Ryujinx.Common;
using Ryujinx.Common.Logging;
using Ryujinx.Common.Memory;
using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;
using static ARMeilleure.Translation.PTC.PtcFormatter;
using Timer = System.Timers.Timer;

namespace ARMeilleure.Translation.PTC
{
    class PtcProfiler
    {
        private const string OuterHeaderMagicString = "Pohd\0\0\0\0";

        private const uint InternalVersion = 7007; //! Not to be incremented manually for each change to the ARMeilleure project.

        private static readonly uint[] _migrateInternalVersions = 
        [
            1866,
            5518,
        ];

        private const int SaveInterval = 30; // Seconds.

        private const CompressionLevel SaveCompressionLevel = CompressionLevel.Fastest;

        private readonly Ptc _ptc;

        private readonly Timer _timer;

        private readonly ulong _outerHeaderMagic;

        private readonly ManualResetEvent _waitEvent;

        private readonly Lock _lock = new();

        private bool _disposed;

        private Hash128 _lastHash;

        public Dictionary<ulong, FuncProfile> ProfiledFuncs { get; private set; }

        public bool Enabled { get; private set; }

        public ulong StaticCodeStart { get; set; }
        public ulong StaticCodeSize { get; set; }

        public PtcProfiler(Ptc ptc)
        {
            _ptc = ptc;

            _timer = new Timer(SaveInterval.Seconds());
            _timer.Elapsed += TimerElapsed;

            _outerHeaderMagic = BinaryPrimitives.ReadUInt64LittleEndian(EncodingCache.UTF8NoBOM.GetBytes(OuterHeaderMagicString).AsSpan());

            _waitEvent = new ManualResetEvent(true);

            _disposed = false;

            ProfiledFuncs = new Dictionary<ulong, FuncProfile>();

            Enabled = false;
        }

        private void TimerElapsed(object _, ElapsedEventArgs __) 
            => new Thread(PreSave) { Name = "Ptc.DiskWriter" }.Start();

        public void AddEntry(ulong address, ExecutionMode mode, bool highCq, bool blacklist = false)
        {
            if (IsAddressInStaticCodeRange(address))
            {
                Debug.Assert(!highCq);

                if (blacklist)
                {
                    lock (_lock)
                    {
                        ProfiledFuncs[address] = new FuncProfile(mode, highCq: false, true);
                    }
                }
                else
                {
                    lock (_lock)
                    {
                        ProfiledFuncs.TryAdd(address, new FuncProfile(mode, highCq: false, false));
                    }
                }
            }
        }

        public void UpdateEntry(ulong address, ExecutionMode mode, bool highCq, bool? blacklist = null)
        {
            if (IsAddressInStaticCodeRange(address))
            {
                Debug.Assert(highCq);

                lock (_lock)
                {
                    Debug.Assert(ProfiledFuncs.ContainsKey(address));

                    ProfiledFuncs[address] = new FuncProfile(mode, highCq: true, blacklist ?? ProfiledFuncs[address].Blacklist);
                }
            }
        }

        public bool IsAddressInStaticCodeRange(ulong address)
        {
            return address >= StaticCodeStart && address < StaticCodeStart + StaticCodeSize;
        }

        public ConcurrentQueue<(ulong address, FuncProfile funcProfile)> GetProfiledFuncsToTranslate(TranslatorCache<TranslatedFunction> funcs)
        {
            ConcurrentQueue<(ulong address, FuncProfile funcProfile)> profiledFuncsToTranslate = new();

            foreach (KeyValuePair<ulong, FuncProfile> profiledFunc in ProfiledFuncs)
            {
                if (!funcs.ContainsKey(profiledFunc.Key) && !profiledFunc.Value.Blacklist)
                {
                    profiledFuncsToTranslate.Enqueue((profiledFunc.Key, profiledFunc.Value));
                }
            }

            return profiledFuncsToTranslate;
        }

        public void ClearEntries()
        {
            ProfiledFuncs.Clear();
            ProfiledFuncs.TrimExcess();
        }

        public List<ulong> GetBlacklistedFunctions()
        {
            List<ulong> funcs = [];

            foreach ((ulong ptr, FuncProfile funcProfile) in ProfiledFuncs)
            {
                if (!funcProfile.Blacklist)
                    continue;

                if (!funcs.Contains(ptr)) 
                    funcs.Add(ptr);
            }

            return funcs;
        }

        public void PreLoad()
        {
            _lastHash = default;

            string fileNameActual = $"{_ptc.CachePathActual}.info";
            string fileNameBackup = $"{_ptc.CachePathBackup}.info";

            FileInfo fileInfoActual = new(fileNameActual);
            FileInfo fileInfoBackup = new(fileNameBackup);

            if (fileInfoActual.Exists && fileInfoActual.Length != 0L)
            {
                if (!Load(fileNameActual, false))
                {
                    if (fileInfoBackup.Exists && fileInfoBackup.Length != 0L)
                    {
                        Load(fileNameBackup, true);
                    }
                }
            }
            else if (fileInfoBackup.Exists && fileInfoBackup.Length != 0L)
            {
                Load(fileNameBackup, true);
            }
        }

        private bool Load(string fileName, bool isBackup)
        {
            using (FileStream compressedStream = new(fileName, FileMode.Open))
            using (DeflateStream deflateStream = new(compressedStream, CompressionMode.Decompress, true))
            {
                OuterHeader outerHeader = DeserializeStructure<OuterHeader>(compressedStream);

                if (!outerHeader.IsHeaderValid())
                {
                    InvalidateCompressedStream(compressedStream);

                    return false;
                }

                if (outerHeader.Magic != _outerHeaderMagic)
                {
                    InvalidateCompressedStream(compressedStream);

                    return false;
                }

                if (outerHeader.InfoFileVersion != InternalVersion && !_migrateInternalVersions.Contains(outerHeader.InfoFileVersion))
                {
                    InvalidateCompressedStream(compressedStream);

                    return false;
                }

                if (outerHeader.Endianness != Ptc.GetEndianness())
                {
                    InvalidateCompressedStream(compressedStream);

                    return false;
                }

                using MemoryStream stream = MemoryStreamManager.Shared.GetStream();
                Debug.Assert(stream.Seek(0L, SeekOrigin.Begin) == 0L && stream.Length == 0L);

                try
                {
                    deflateStream.CopyTo(stream);
                }
                catch
                {
                    InvalidateCompressedStream(compressedStream);

                    return false;
                }

                Debug.Assert(stream.Position == stream.Length);

                stream.Seek(0L, SeekOrigin.Begin);

                Hash128 expectedHash = DeserializeStructure<Hash128>(stream);

                Hash128 actualHash = Hash128.ComputeHash(GetReadOnlySpan(stream));

                if (actualHash != expectedHash)
                {
                    InvalidateCompressedStream(compressedStream);

                    return false;
                }

                Func<ulong, FuncProfile, (ulong, FuncProfile)> migrateEntryFunc = null;

                switch (outerHeader.InfoFileVersion)
                {
                    case InternalVersion:
                        ProfiledFuncs = Deserialize(stream);
                        break;
                    case 1866:
                        migrateEntryFunc = (address, profile) => (address + 0x500000UL, profile);
                        goto case 5518;
                    case 5518:
                        ProfiledFuncs = DeserializeAddBlacklist(stream, migrateEntryFunc);
                        break;
                    default:
                        Logger.Error?.Print(LogClass.Ptc, $"No migration path for {nameof(outerHeader.InfoFileVersion)} '{outerHeader.InfoFileVersion}'. Discarding cache.");
                        InvalidateCompressedStream(compressedStream);
                        return false;
                }

                Debug.Assert(stream.Position == stream.Length);

                _lastHash = actualHash;
            }

            long fileSize = new FileInfo(fileName).Length;

            Logger.Info?.Print(LogClass.Ptc, $"{(isBackup ? "Loaded Backup Profiling Info" : "Loaded Profiling Info")} (size: {fileSize} bytes, profiled functions: {ProfiledFuncs.Count}).");

            return true;
        }

        private static Dictionary<ulong, FuncProfile> Deserialize(Stream stream, Func<ulong, FuncProfile, (ulong, FuncProfile)> migrateEntryFunc = null)
        {
            if (migrateEntryFunc != null)
            {
                return DeserializeAndUpdateDictionary(stream, DeserializeStructure<FuncProfile>, migrateEntryFunc);
            }

            return DeserializeDictionary<ulong, FuncProfile>(stream, DeserializeStructure<FuncProfile>);
        }

        private static Dictionary<ulong, FuncProfile> DeserializeAddBlacklist(Stream stream, Func<ulong, FuncProfile, (ulong, FuncProfile)> migrateEntryFunc = null)
        {
            if (migrateEntryFunc != null)
            {
                return DeserializeAndUpdateDictionary(stream, (Stream stream) => { return new FuncProfile(DeserializeStructure<FuncProfilePreBlacklist>(stream)); }, migrateEntryFunc);
            }

            return DeserializeDictionary<ulong, FuncProfile>(stream, (Stream stream) => { return new FuncProfile(DeserializeStructure<FuncProfilePreBlacklist>(stream)); });
        }

        private static ReadOnlySpan<byte> GetReadOnlySpan(MemoryStream memoryStream)
        {
            return new(memoryStream.GetBuffer(), (int)memoryStream.Position, (int)memoryStream.Length - (int)memoryStream.Position);
        }

        private static void InvalidateCompressedStream(FileStream compressedStream)
        {
            compressedStream.SetLength(0L);
        }

        private void PreSave()
        {
            _waitEvent.Reset();

            string fileNameActual = $"{_ptc.CachePathActual}.info";
            string fileNameBackup = $"{_ptc.CachePathBackup}.info";

            FileInfo fileInfoActual = new(fileNameActual);

            if (fileInfoActual.Exists && fileInfoActual.Length != 0L)
            {
                File.Copy(fileNameActual, fileNameBackup, true);
            }

            Save(fileNameActual);

            _waitEvent.Set();
        }

        private void Save(string fileName)
        {
            int profiledFuncsCount;

            OuterHeader outerHeader = new()
            {
                Magic = _outerHeaderMagic,

                InfoFileVersion = InternalVersion,
                Endianness = Ptc.GetEndianness(),
            };

            outerHeader.SetHeaderHash();

            using (MemoryStream stream = MemoryStreamManager.Shared.GetStream())
            {
                Debug.Assert(stream.Seek(0L, SeekOrigin.Begin) == 0L && stream.Length == 0L);

                stream.Seek(Unsafe.SizeOf<Hash128>(), SeekOrigin.Begin);

                lock (_lock)
                {
                    Serialize(stream, ProfiledFuncs);

                    profiledFuncsCount = ProfiledFuncs.Count;
                }

                Debug.Assert(stream.Position == stream.Length);

                stream.Seek(Unsafe.SizeOf<Hash128>(), SeekOrigin.Begin);
                Hash128 hash = Hash128.ComputeHash(GetReadOnlySpan(stream));

                stream.Seek(0L, SeekOrigin.Begin);
                SerializeStructure(stream, hash);

                if (hash == _lastHash)
                {
                    return;
                }

                using FileStream compressedStream = new(fileName, FileMode.OpenOrCreate);
                using DeflateStream deflateStream = new(compressedStream, SaveCompressionLevel, true);
                try
                {
                    SerializeStructure(compressedStream, outerHeader);

                    stream.WriteTo(deflateStream);

                    _lastHash = hash;
                }
                catch
                {
                    compressedStream.Position = 0L;

                    _lastHash = default;
                }

                if (compressedStream.Position < compressedStream.Length)
                {
                    compressedStream.SetLength(compressedStream.Position);
                }
            }

            long fileSize = new FileInfo(fileName).Length;

            if (fileSize != 0L)
            {
                Logger.Info?.Print(LogClass.Ptc, $"Saved Profiling Info (size: {fileSize} bytes, profiled functions: {profiledFuncsCount}).");
            }
        }

        private static void Serialize(Stream stream, Dictionary<ulong, FuncProfile> profiledFuncs)
        {
            SerializeDictionary(stream, profiledFuncs, SerializeStructure);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1/*, Size = 29*/)]
        private struct OuterHeader
        {
            public ulong Magic;

            public uint InfoFileVersion;

            public bool Endianness;

            public Hash128 HeaderHash;

            public void SetHeaderHash()
            {
                Span<OuterHeader> spanHeader = MemoryMarshal.CreateSpan(ref this, 1);

                HeaderHash = Hash128.ComputeHash(MemoryMarshal.AsBytes(spanHeader)[..(Unsafe.SizeOf<OuterHeader>() - Unsafe.SizeOf<Hash128>())]);
            }

            public bool IsHeaderValid()
            {
                Span<OuterHeader> spanHeader = MemoryMarshal.CreateSpan(ref this, 1);

                return Hash128.ComputeHash(MemoryMarshal.AsBytes(spanHeader)[..(Unsafe.SizeOf<OuterHeader>() - Unsafe.SizeOf<Hash128>())]) == HeaderHash;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1/*, Size = 6*/)]
        public struct FuncProfile
        {
            public ExecutionMode Mode;
            public bool HighCq;
            public bool Blacklist;

            public FuncProfile(ExecutionMode mode, bool highCq, bool blacklist)
            {
                Mode = mode;
                HighCq = highCq;
                Blacklist = blacklist;
            }

            public FuncProfile(FuncProfilePreBlacklist fp)
            {
                Mode = fp.Mode;
                HighCq = fp.HighCq;
                Blacklist = false;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1/*, Size = 5*/)]
        public struct FuncProfilePreBlacklist
        {
            public ExecutionMode Mode;
            public bool HighCq;

            public FuncProfilePreBlacklist(ExecutionMode mode, bool highCq)
            {
                Mode = mode;
                HighCq = highCq;
            }
        }

        public void Start()
        {
            if (_ptc.State == PtcState.Enabled ||
                _ptc.State == PtcState.Continuing)
            {
                Enabled = true;

                _timer.Enabled = true;
            }
        }

        public void Stop()
        {
            Enabled = false;

            if (!_disposed)
            {
                _timer.Enabled = false;
            }
        }

        public void Wait()
        {
            _waitEvent.WaitOne();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                _timer.Elapsed -= TimerElapsed;
                _timer.Dispose();

                Wait();
                _waitEvent.Dispose();
            }
        }
    }
}
