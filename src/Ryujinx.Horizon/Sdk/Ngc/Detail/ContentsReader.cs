using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Fs;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace Ryujinx.Horizon.Sdk.Ngc.Detail
{
    class ContentsReader : IDisposable
    {
        private const string MountName = "NgWord";
        private const string VersionFilePath = $"{MountName}:/version.dat";
        private const ulong DataId = 0x100000000000823UL;

        private enum AcType
        {
            AcNotB,
            AcB1,
            AcB2,
            AcSimilarForm,
            TableSimilarForm,
        }

        private readonly IFsClient _fsClient;
        private readonly Lock _lock = new();
        private bool _intialized;
        private ulong _cacheSize;

        public ContentsReader(IFsClient fsClient)
        {
            _fsClient = fsClient;
        }

        private static void MakeMountPoint(out string path, AcType type, int regionIndex)
        {
            path = null;

            switch (type)
            {
                case AcType.AcNotB:
                    if (regionIndex < 0)
                    {
                        path = $"{MountName}:/ac_common_not_b_nx";
                    }
                    else
                    {
                        path = $"{MountName}:/ac_{regionIndex}_not_b_nx";
                    }
                    break;
                case AcType.AcB1:
                    if (regionIndex < 0)
                    {
                        path = $"{MountName}:/ac_common_b1_nx";
                    }
                    else
                    {
                        path = $"{MountName}:/ac_{regionIndex}_b1_nx";
                    }
                    break;
                case AcType.AcB2:
                    if (regionIndex < 0)
                    {
                        path = $"{MountName}:/ac_common_b2_nx";
                    }
                    else
                    {
                        path = $"{MountName}:/ac_{regionIndex}_b2_nx";
                    }
                    break;
                case AcType.AcSimilarForm:
                    path = $"{MountName}:/ac_similar_form_nx";
                    break;
                case AcType.TableSimilarForm:
                    path = $"{MountName}:/table_similar_form_nx";
                    break;
            }
        }

        public Result Initialize(ulong cacheSize)
        {
            lock (_lock)
            {
                if (_intialized)
                {
                    return Result.Success;
                }

                Result result = _fsClient.QueryMountSystemDataCacheSize(out long dataCacheSize, DataId);
                if (result.IsFailure)
                {
                    return result;
                }

                if (cacheSize < (ulong)dataCacheSize)
                {
                    return NgcResult.InvalidSize;
                }

                result = _fsClient.MountSystemData(MountName, DataId);
                if (result.IsFailure)
                {
                    // Official firmware would return the result here,
                    // we don't to support older firmware where the archive didn't exist yet.
                    return Result.Success;
                }

                _cacheSize = cacheSize;
                _intialized = true;

                return Result.Success;
            }
        }

        public Result Reload()
        {
            lock (_lock)
            {
                if (!_intialized)
                {
                    return Result.Success;
                }

                _fsClient.Unmount(MountName);

                Result result = Result.Success;

                try
                {
                    result = _fsClient.QueryMountSystemDataCacheSize(out long cacheSize, DataId);
                    if (result.IsFailure)
                    {
                        return result;
                    }

                    if (_cacheSize < (ulong)cacheSize)
                    {
                        result = NgcResult.InvalidSize;
                        return NgcResult.InvalidSize;
                    }

                    result = _fsClient.MountSystemData(MountName, DataId);
                    if (result.IsFailure)
                    {
                        return result;
                    }
                }
                finally
                {
                    if (result.IsFailure)
                    {
                        _intialized = false;
                        _cacheSize = 0;
                    }
                }
            }

            return Result.Success;
        }

        private Result GetFileSize(out long size, string filePath)
        {
            size = 0;

            lock (_lock)
            {
                Result result = _fsClient.OpenFile(out FileHandle handle, filePath, OpenMode.Read);
                if (result.IsFailure)
                {
                    return result;
                }

                try
                {
                    result = _fsClient.GetFileSize(out size, handle);
                    if (result.IsFailure)
                    {
                        return result;
                    }
                }
                finally
                {
                    _fsClient.CloseFile(handle);
                }
            }

            return Result.Success;
        }

        private Result GetFileContent(Span<byte> destination, string filePath)
        {
            lock (_lock)
            {
                Result result = _fsClient.OpenFile(out FileHandle handle, filePath, OpenMode.Read);
                if (result.IsFailure)
                {
                    return result;
                }

                try
                {
                    result = _fsClient.ReadFile(handle, 0, destination);
                    if (result.IsFailure)
                    {
                        return result;
                    }
                }
                finally
                {
                    _fsClient.CloseFile(handle);
                }
            }

            return Result.Success;
        }

        public Result GetVersionDataSize(out long size)
        {
            return GetFileSize(out size, VersionFilePath);
        }

        public Result GetVersionData(Span<byte> destination)
        {
            return GetFileContent(destination, VersionFilePath);
        }

        public Result ReadDictionaries(out AhoCorasick partialWordsTrie, out AhoCorasick completeWordsTrie, out AhoCorasick delimitedWordsTrie, int regionIndex)
        {
            completeWordsTrie = null;
            delimitedWordsTrie = null;

            MakeMountPoint(out string partialWordsTriePath, AcType.AcNotB, regionIndex);
            MakeMountPoint(out string completeWordsTriePath, AcType.AcB1, regionIndex);
            MakeMountPoint(out string delimitedWordsTriePath, AcType.AcB2, regionIndex);

            Result result = ReadDictionary(out partialWordsTrie, partialWordsTriePath);
            if (result.IsFailure)
            {
                return NgcResult.DataAccessError;
            }

            result = ReadDictionary(out completeWordsTrie, completeWordsTriePath);
            if (result.IsFailure)
            {
                return NgcResult.DataAccessError;
            }

            return ReadDictionary(out delimitedWordsTrie, delimitedWordsTriePath);
        }

        public Result ReadSimilarFormDictionary(out AhoCorasick similarFormTrie)
        {
            MakeMountPoint(out string similarFormTriePath, AcType.AcSimilarForm, 0);

            return ReadDictionary(out similarFormTrie, similarFormTriePath);
        }

        public Result ReadSimilarFormTable(out SimilarFormTable similarFormTable)
        {
            similarFormTable = null;

            MakeMountPoint(out string similarFormTablePath, AcType.TableSimilarForm, 0);

            Result result = ReadGZipCompressedArchive(out byte[] data, similarFormTablePath);
            if (result.IsFailure)
            {
                return result;
            }

            BinaryReader reader = new(data);
            SimilarFormTable table = new();

            if (!table.Import(ref reader))
            {
                // Official firmware doesn't return an error here and just assumes the import was successful.
                return NgcResult.DataAccessError;
            }

            similarFormTable = table;

            return Result.Success;
        }

        public static Result ReadNotSeparatorDictionary(out AhoCorasick notSeparatorTrie)
        {
            notSeparatorTrie = null;

            BinaryReader reader = new(EmbeddedTries.NotSeparatorTrie);
            AhoCorasick ac = new();

            if (!ac.Import(ref reader))
            {
                // Official firmware doesn't return an error here and just assumes the import was successful.
                return NgcResult.DataAccessError;
            }

            notSeparatorTrie = ac;

            return Result.Success;
        }

        private Result ReadDictionary(out AhoCorasick trie, string path)
        {
            trie = null;

            Result result = ReadGZipCompressedArchive(out byte[] data, path);
            if (result.IsFailure)
            {
                return result;
            }

            BinaryReader reader = new(data);
            AhoCorasick ac = new();

            if (!ac.Import(ref reader))
            {
                // Official firmware doesn't return an error here and just assumes the import was successful.
                return NgcResult.DataAccessError;
            }

            trie = ac;

            return Result.Success;
        }

        private Result ReadGZipCompressedArchive(out byte[] data, string filePath)
        {
            data = null;

            Result result = _fsClient.OpenFile(out FileHandle handle, filePath, OpenMode.Read);
            if (result.IsFailure)
            {
                return result;
            }

            try
            {
                result = _fsClient.GetFileSize(out long fileSize, handle);
                if (result.IsFailure)
                {
                    return result;
                }

                data = new byte[fileSize];

                result = _fsClient.ReadFile(handle, 0, data.AsSpan());
                if (result.IsFailure)
                {
                    return result;
                }
            }
            finally
            {
                _fsClient.CloseFile(handle);
            }

            try
            {
                data = DecompressGZipCompressedStream(data);
            }
            catch (InvalidDataException)
            {
                // Official firmware returns a different error, but it is translated to this error on the caller.
                return NgcResult.DataAccessError;
            }

            return Result.Success;
        }

        private static byte[] DecompressGZipCompressedStream(byte[] data)
        {
            using MemoryStream input = new(data);
            using GZipStream gZipStream = new(input, CompressionMode.Decompress);
            using MemoryStream output = new();

            gZipStream.CopyTo(output);

            return output.ToArray();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_lock)
                {
                    if (!_intialized)
                    {
                        return;
                    }

                    _fsClient.Unmount(MountName);
                    _intialized = false;
                }
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
