// Uncomment the line below to ensure XCIFileTrimmer does not modify files
//#define XCI_TRIMMER_READ_ONLY_MODE

using Gommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Ryujinx.Common.Utilities
{
    public sealed class XCIFileTrimmer
    {
        private const long BytesInAMegabyte = 1024 * 1024;
        private const int BufferSize = 8 * (int)BytesInAMegabyte;

        private const long CartSizeMBinFormattedGB = 952;
        private const int CartKeyAreaSize = 0x1000;
        private const byte PaddingByte = 0xFF;
        private const int HeaderFilePos = 0x100;
        private const int CartSizeFilePos = 0x10D;
        private const int DataSizeFilePos = 0x118;
        private const string HeaderMagicValue = "HEAD";

        /// <summary>
        /// Cartridge Sizes (ByteIdentifier, SizeInGB)
        /// </summary>
        private static readonly Dictionary<byte, long> _cartSizesGB = new()
        {
            { 0xFA, 1 },
            { 0xF8, 2 },
            { 0xF0, 4 },
            { 0xE0, 8 },
            { 0xE1, 16 },
            { 0xE2, 32 }
        };

        private static long RecordsToByte(long records)
        {
            return 512 + (records * 512);
        }

        public static bool CanTrim(string filename, ILog log = null)
        {
            if (Path.GetExtension(filename).Equals(".XCI", StringComparison.InvariantCultureIgnoreCase))
            {
                XCIFileTrimmer trimmer = new(filename, log);
                return trimmer.CanBeTrimmed;
            }

            return false;
        }

        public static bool CanUntrim(string filename, ILog log = null)
        {
            if (Path.GetExtension(filename).Equals(".XCI", StringComparison.InvariantCultureIgnoreCase))
            {
                XCIFileTrimmer trimmer = new(filename, log);
                return trimmer.CanBeUntrimmed;
            }

            return false;
        }

        private ILog _log;
        private string _filename;
        private FileStream _fileStream;
        private BinaryReader _binaryReader;
        private long _offsetB, _dataSizeB, _cartSizeB, _fileSizeB;
        private bool _fileOK = true;
        private bool _freeSpaceChecked = false;
        private bool _freeSpaceValid = false;

        public enum OperationOutcome
        {
            Undetermined,
            InvalidXCIFile,
            NoTrimNecessary,
            NoUntrimPossible,
            FreeSpaceCheckFailed,
            FileIOWriteError,
            ReadOnlyFileCannotFix,
            FileSizeChanged,
            Successful,
            Cancelled
        }

        public enum LogType
        {
            Info,
            Warn,
            Error,
            Progress
        }

        public interface ILog
        {
            public void Write(LogType logType, string text);
            public void Progress(long current, long total, string text, bool complete);
        }

        public bool FileOK => _fileOK;
        public bool Trimmed => _fileOK && FileSizeB < UntrimmedFileSizeB;
        public bool ContainsKeyArea => _offsetB != 0;
        public bool CanBeTrimmed => _fileOK && FileSizeB > TrimmedFileSizeB;
        public bool CanBeUntrimmed => _fileOK && FileSizeB < UntrimmedFileSizeB;
        public bool FreeSpaceChecked => _fileOK && _freeSpaceChecked;
        public bool FreeSpaceValid => _fileOK && _freeSpaceValid;
        public long DataSizeB => _dataSizeB;
        public long CartSizeB => _cartSizeB;
        public long FileSizeB => _fileSizeB;
        public long DiskSpaceSavedB => CartSizeB - FileSizeB;
        public long DiskSpaceSavingsB => CartSizeB - DataSizeB;
        public long TrimmedFileSizeB => _offsetB + _dataSizeB;
        public long UntrimmedFileSizeB => _offsetB + _cartSizeB;

        public ILog Log
        {
            get => _log;
            set => _log = value;
        }

        public String Filename
        {
            get => _filename;
            set
            {
                _filename = value;
                Reset();
            }
        }

        public long Pos
        {
            get => _fileStream.Position;
            set => _fileStream.Position = value;
        }

        public XCIFileTrimmer(string path, ILog log = null)
        {
            Log = log;
            Filename = path;
            ReadHeader();
        }

        public void CheckFreeSpace(CancellationToken? cancelToken = null)
        {
            if (FreeSpaceChecked)
                return;

            try
            {
                if (CanBeTrimmed)
                {
                    _freeSpaceValid = false;

                    OpenReaders();

                    try
                    {
                        Pos = TrimmedFileSizeB;
                        bool freeSpaceValid = true;
                        long readSizeB = FileSizeB - TrimmedFileSizeB;

                        Stopwatch timedSw = Lambda.Timed(() =>
                        {
                            freeSpaceValid = CheckPadding(readSizeB, cancelToken);
                        });

                        if (timedSw.Elapsed.TotalSeconds > 0)
                        {
                            Log?.Write(LogType.Info, $"Checked at {readSizeB / (double)XCIFileTrimmer.BytesInAMegabyte / timedSw.Elapsed.TotalSeconds:N} Mb/sec");
                        }

                        if (freeSpaceValid)
                            Log?.Write(LogType.Info, "Free space is valid");

                        _freeSpaceValid = freeSpaceValid;
                    }
                    finally
                    {
                        CloseReaders();
                    }

                }
                else
                {
                    Log?.Write(LogType.Warn, "There is no free space to check.");
                    _freeSpaceValid = false;
                }
            }
            finally
            {
                _freeSpaceChecked = true;
            }
        }

        private bool CheckPadding(long readSizeB, CancellationToken? cancelToken = null)
        {
            long maxReads = readSizeB / XCIFileTrimmer.BufferSize;
            long read = 0;
            byte[] buffer = new byte[BufferSize];

            while (true)
            {
                if (cancelToken.HasValue && cancelToken.Value.IsCancellationRequested) 
                {
                    return false;
                }

                int bytes = _fileStream.Read(buffer, 0, XCIFileTrimmer.BufferSize);
                if (bytes == 0)
                    break;

                Log?.Progress(read, maxReads, "Verifying file can be trimmed", false);
                if (buffer.Take(bytes).AsParallel().Any(b => b != XCIFileTrimmer.PaddingByte))
                {
                    Log?.Write(LogType.Warn, "Free space is NOT valid");
                    return false;
                }

                read++;
            }

            return true;
        }

        private void Reset()
        {
            _freeSpaceChecked = false;
            _freeSpaceValid = false;
            ReadHeader();
        }

        public OperationOutcome Trim(CancellationToken? cancelToken = null)
        {
            if (!FileOK)
            {
                return OperationOutcome.InvalidXCIFile;
            }

            if (!CanBeTrimmed)
            {
                return OperationOutcome.NoTrimNecessary;
            }

            if (!FreeSpaceChecked)
            {
                CheckFreeSpace(cancelToken);
            }

            if (!FreeSpaceValid)
            {
                if (cancelToken.HasValue && cancelToken.Value.IsCancellationRequested)
                {
                    return OperationOutcome.Cancelled;
                }
                else 
                {
                    return OperationOutcome.FreeSpaceCheckFailed;
                }
            }

            Log?.Write(LogType.Info, "Trimming...");

            try
            {
                FileInfo info = new(Filename);
                if ((info.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    try
                    {
                        Log?.Write(LogType.Info, "Attempting to remove ReadOnly attribute");
                        File.SetAttributes(Filename, info.Attributes & ~FileAttributes.ReadOnly);
                    }
                    catch (Exception e)
                    {
                        Log?.Write(LogType.Error, e.ToString());
                        return OperationOutcome.ReadOnlyFileCannotFix;
                    }
                }

                if (info.Length != FileSizeB)
                {
                    Log?.Write(LogType.Error, "File size has changed, cannot safely trim.");
                    return OperationOutcome.FileSizeChanged;
                }

                FileStream outfileStream = new(_filename, FileMode.Open, FileAccess.Write, FileShare.Write);

                try
                {

#if !XCI_TRIMMER_READ_ONLY_MODE
                        outfileStream.SetLength(TrimmedFileSizeB);
#endif
                    return OperationOutcome.Successful;
                }
                finally
                {
                    outfileStream.Close();
                    Reset();
                }
            }
            catch (Exception e)
            {
                Log?.Write(LogType.Error, e.ToString());
                return OperationOutcome.FileIOWriteError;
            }
        }

        public OperationOutcome Untrim(CancellationToken? cancelToken = null)
        {
            if (!FileOK)
            {
                return OperationOutcome.InvalidXCIFile;
            }

            if (!CanBeUntrimmed)
            {
                return OperationOutcome.NoUntrimPossible;
            }

            try
            {
                Log?.Write(LogType.Info, "Untrimming...");

                FileInfo info = new(Filename);
                if ((info.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    try
                    {
                        Log?.Write(LogType.Info, "Attempting to remove ReadOnly attribute");
                        File.SetAttributes(Filename, info.Attributes & ~FileAttributes.ReadOnly);
                    }
                    catch (Exception e)
                    {
                        Log?.Write(LogType.Error, e.ToString());
                        return OperationOutcome.ReadOnlyFileCannotFix;
                    }
                }

                if (info.Length != FileSizeB)
                {
                    Log?.Write(LogType.Error, "File size has changed, cannot safely untrim.");
                    return OperationOutcome.FileSizeChanged;
                }

                FileStream outfileStream = new(_filename, FileMode.Append, FileAccess.Write, FileShare.Write);
                long bytesToWriteB = UntrimmedFileSizeB - FileSizeB;

                try
                {
                    Stopwatch timedSw = Lambda.Timed(() =>
                    {
                        WritePadding(outfileStream, bytesToWriteB, cancelToken);
                    });

                    if (timedSw.Elapsed.TotalSeconds > 0)
                    {
                        Log?.Write(LogType.Info, $"Wrote at {bytesToWriteB / (double)XCIFileTrimmer.BytesInAMegabyte / timedSw.Elapsed.TotalSeconds:N} Mb/sec");
                    }

                    if (cancelToken.HasValue && cancelToken.Value.IsCancellationRequested)
                    {
                        return OperationOutcome.Cancelled;
                    }
                    else 
                    {
                        return OperationOutcome.Successful;
                    }
                }
                finally
                {
                    outfileStream.Close();
                    Reset();
                }
            }
            catch (Exception e)
            {
                Log?.Write(LogType.Error, e.ToString());
                return OperationOutcome.FileIOWriteError;
            }
        }

        private void WritePadding(FileStream outfileStream, long bytesToWriteB, CancellationToken? cancelToken = null)
        {
            long bytesLeftToWriteB = bytesToWriteB;
            long writes = bytesLeftToWriteB / XCIFileTrimmer.BufferSize;
            int write = 0;

            try
            {
                byte[] buffer = new byte[BufferSize];
                Array.Fill<byte>(buffer, XCIFileTrimmer.PaddingByte);

                while (bytesLeftToWriteB > 0)
                {
                    if (cancelToken.HasValue && cancelToken.Value.IsCancellationRequested)
                    {
                        return;
                    }

                    long bytesToWrite = Math.Min(XCIFileTrimmer.BufferSize, bytesLeftToWriteB);
                    
#if !XCI_TRIMMER_READ_ONLY_MODE
                        outfileStream.Write(buffer, 0, (int)bytesToWrite);
#endif

                    bytesLeftToWriteB -= bytesToWrite;
                    Log?.Progress(write, writes, "Writing padding data...", false);
                    write++;
                }
            }
            finally
            {
                Log?.Progress(write, writes, "Writing padding data...", true);
            }
        }

        private void OpenReaders()
        {
            if (_binaryReader == null)
            {
                _fileStream = new FileStream(_filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                _binaryReader = new BinaryReader(_fileStream);
            }
        }

        private void CloseReaders()
        {
            if (_binaryReader != null && _binaryReader.BaseStream != null)
                _binaryReader.Close();
            _binaryReader = null;
            _fileStream = null;
            GC.Collect();
        }

        private void ReadHeader()
        {
            try
            {
                OpenReaders();

                try
                {
                    // Attempt without key area
                    bool success = CheckAndReadHeader(false);

                    if (!success)
                    {
                        // Attempt with key area
                        success = CheckAndReadHeader(true);
                    }

                    _fileOK = success;
                }
                finally
                {
                    CloseReaders();
                }
            }
            catch (Exception ex)
            {
                Log?.Write(LogType.Error, ex.Message);
                _fileOK = false;
                _dataSizeB = 0;
                _cartSizeB = 0;
                _fileSizeB = 0;
                _offsetB = 0;
            }
        }

        private bool CheckAndReadHeader(bool assumeKeyArea)
        {
            // Read file size
            _fileSizeB = _fileStream.Length;
            if (_fileSizeB < 32 * 1024)
            {
                Log?.Write(LogType.Error, "The source file doesn't look like an XCI file as the data size is too small");
                return false;
            }

            // Setup offset
            _offsetB = (long)(assumeKeyArea ? XCIFileTrimmer.CartKeyAreaSize : 0);

            // Check header
            Pos = _offsetB + XCIFileTrimmer.HeaderFilePos;
            string head = System.Text.Encoding.ASCII.GetString(_binaryReader.ReadBytes(4));
            if (head != XCIFileTrimmer.HeaderMagicValue)
            {
                if (!assumeKeyArea)
                {
                    Log?.Write(LogType.Warn, $"Incorrect header found, file mat contain a key area...");
                }
                else
                {
                    Log?.Write(LogType.Error, "The source file doesn't look like an XCI file as the header is corrupted");
                }

                return false;
            }

            // Read Cart Size
            Pos = _offsetB + XCIFileTrimmer.CartSizeFilePos;
            byte cartSizeId = _binaryReader.ReadByte();
            if (!_cartSizesGB.TryGetValue(cartSizeId, out long cartSizeNGB))
            {
                Log?.Write(LogType.Error, $"The source file doesn't look like an XCI file as the Cartridge Size is incorrect (0x{cartSizeId:X2})");
                return false;
            }
            _cartSizeB = cartSizeNGB * XCIFileTrimmer.CartSizeMBinFormattedGB * XCIFileTrimmer.BytesInAMegabyte;

            // Read data size
            Pos = _offsetB + XCIFileTrimmer.DataSizeFilePos;
            long records = (long)BitConverter.ToUInt32(_binaryReader.ReadBytes(4), 0);
            _dataSizeB = RecordsToByte(records);

            return true;
        }
    }
}
