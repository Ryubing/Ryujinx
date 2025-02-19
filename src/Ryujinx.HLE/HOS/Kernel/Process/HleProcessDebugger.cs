using Ryujinx.Cpu;
using Ryujinx.HLE.HOS.Diagnostics.Demangler;
using Ryujinx.HLE.HOS.Kernel.Memory;
using Ryujinx.HLE.HOS.Kernel.Threading;
using Ryujinx.HLE.Loaders.Elf;
using Ryujinx.Memory;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Ryujinx.HLE.HOS.Kernel.Process
{
    class HleProcessDebugger
    {
        private const int Mod0 = 'M' << 0 | 'O' << 8 | 'D' << 16 | '0' << 24;

        private readonly KProcess _owner;

        private class Image
        {
            public ulong BaseAddress { get; }
            public ulong Size { get; }
            public ulong EndAddress => BaseAddress + Size;

            public ElfSymbol[] Symbols { get; }

            public Image(ulong baseAddress, ulong size, ElfSymbol[] symbols)
            {
                BaseAddress = baseAddress;
                Size = size;
                Symbols = symbols;
            }
        }

        private readonly List<Image> _images;

        private int _loaded;

        public HleProcessDebugger(KProcess owner)
        {
            _owner = owner;

            _images = [];
        }

        public string GetGuestStackTrace(KThread thread)
        {
            EnsureLoaded();

            IExecutionContext context = thread.Context;

            StringBuilder trace = new();

            trace.AppendLine($"Process: {_owner.Name}, PID: {_owner.Pid}");

            void AppendTrace(ulong address)
            {
                if (AnalyzePointer(out PointerInfo info, address, thread))
                {
                    trace.AppendLine($"   0x{address:x16}\t{info.ImageDisplay}\t{info.SubDisplay}");
                }
                else
                {
                    trace.AppendLine($"   0x{address:x16}");
                }
            }

            if (context.IsAarch32)
            {
                ulong framePointer = context.GetX(11);

                while (framePointer != 0)
                {
                    if ((framePointer & 3) != 0 ||
                        !_owner.CpuMemory.IsMapped(framePointer) ||
                        !_owner.CpuMemory.IsMapped(framePointer + 4))
                    {
                        break;
                    }

                    AppendTrace(_owner.CpuMemory.Read<uint>(framePointer + 4));

                    framePointer = _owner.CpuMemory.Read<uint>(framePointer);
                }
            }
            else
            {
                ulong framePointer = context.GetX(29);

                while (framePointer != 0)
                {
                    if ((framePointer & 7) != 0 ||
                        !_owner.CpuMemory.IsMapped(framePointer) ||
                        !_owner.CpuMemory.IsMapped(framePointer + 8))
                    {
                        break;
                    }

                    AppendTrace(_owner.CpuMemory.Read<ulong>(framePointer + 8));

                    framePointer = _owner.CpuMemory.Read<ulong>(framePointer);
                }
            }

            return trace.ToString();
        }

        public string GetCpuRegisterPrintout(KThread thread)
        {
            EnsureLoaded();

            IExecutionContext context = thread.Context;

            StringBuilder sb = new();

            string GetReg(int x)
            {
                ulong v = x == 32 ? context.Pc : context.GetX(x);
                if (!AnalyzePointer(out PointerInfo info, v, thread))
                {
                    return $"0x{v:x16}";
                }
                else
                {
                    if (!string.IsNullOrEmpty(info.ImageName))
                    {
                        return $"0x{v:x16} ({info.ImageDisplay})\t=> {info.SubDisplay}";
                    }
                    else
                    {
                        return $"0x{v:x16} ({info.SpDisplay})";
                    }
                }
            }

            for (int i = 0; i <= 28; i++)
            {
                sb.AppendLine($"\tX[{i:d2}]:\t{GetReg(i)}");
            }
            sb.AppendLine($"\tFP:\t{GetReg(29)}");
            sb.AppendLine($"\tLR:\t{GetReg(30)}");
            sb.AppendLine($"\tSP:\t{GetReg(31)}");
            sb.AppendLine($"\tPC:\t{GetReg(32)}");

            return sb.ToString();
        }

        private static bool TryGetSubName(Image image, ulong address, out ElfSymbol symbol)
        {
            address -= image.BaseAddress;

            int left = 0;
            int right = image.Symbols.Length - 1;

            while (left <= right)
            {
                int size = right - left;

                int middle = left + (size >> 1);

                symbol = image.Symbols[middle];

                ulong endAddr = symbol.Value + symbol.Size;

                if (address >= symbol.Value && address < endAddr)
                {
                    return true;
                }

                if (address < symbol.Value)
                {
                    right = middle - 1;
                }
                else
                {
                    left = middle + 1;
                }
            }

            symbol = default;

            return false;
        }

        struct PointerInfo
        {
            public string ImageName;
            public string SubName;

            public ulong Offset;
            public ulong SubOffset;

            public readonly string ImageDisplay => $"{ImageName}:0x{Offset:x4}";
            public readonly string SubDisplay => SubOffset == 0 ? SubName : $"{SubName}:0x{SubOffset:x4}";
            public readonly string SpDisplay => SubOffset == 0 ? "SP" : $"SP:-0x{SubOffset:x4}";
        }

        private bool AnalyzePointer(out PointerInfo info, ulong address, KThread thread)
        {
            if (AnalyzePointerFromImages(out info, address))
            {
                return true;
            }

            if (AnalyzePointerFromStack(out info, address, thread))
            {
                return true;
            }

            return false;
        }

        private bool AnalyzePointerFromImages(out PointerInfo info, ulong address)
        {
            info = default;

            Image image = GetImage(address, out int imageIndex);

            if (image == null)
            {
                // Value isn't a pointer to a known image...
                return false;
            }

            info.Offset = address - image.BaseAddress;

            // Try to find what this pointer is referring to
            if (TryGetSubName(image, address, out ElfSymbol symbol))
            {
                info.SubName = symbol.Name;

                // Demangle string if possible
                if (info.SubName.StartsWith("_Z"))
                {
                    info.SubName = Demangler.Parse(info.SubName);
                }
                info.SubOffset = info.Offset - symbol.Value;
            }
            else
            {
                info.SubName = string.Empty;
            }

            info.ImageName = GetGuessedNsoNameFromIndex(imageIndex);

            return true;
        }

        private bool AnalyzePointerFromStack(out PointerInfo info, ulong address, KThread thread)
        {
            info = default;

            ulong sp = thread.Context.GetX(31);
            KMemoryInfo memoryInfo = _owner.MemoryManager.QueryMemory(address);
            MemoryState memoryState = memoryInfo.State;

            if (!memoryState.HasFlag(MemoryState.Stack)) // Is this pointer within the stack?
            {
                return false;
            }

            info.SubOffset = address - sp;

            return true;
        }

        private Image GetImage(ulong address, out int index)
        {
            lock (_images)
            {
                for (index = _images.Count - 1; index >= 0; index--)
                {
                    if (address >= _images[index].BaseAddress && address < _images[index].EndAddress)
                    {
                        return _images[index];
                    }
                }
            }

            return null;
        }

        private string GetGuessedNsoNameFromIndex(int index)
        {
            if ((uint)index > 11)
            {
                return "???";
            }

            if (index == 0)
            {
                return "rtld";
            }
            else if (index == 1)
            {
                return "main";
            }
            else if (index == GetImagesCount() - 1)
            {
                return "sdk";
            }
            else
            {
                return "subsdk" + (index - 2);
            }
        }

        private int GetImagesCount()
        {
            lock (_images)
            {
                return _images.Count;
            }
        }

        private void EnsureLoaded()
        {
            if (Interlocked.CompareExchange(ref _loaded, 1, 0) == 0)
            {
                ScanMemoryForTextSegments();
            }
        }

        private void ScanMemoryForTextSegments()
        {
            ulong oldAddress = 0;
            ulong address = 0;

            while (address >= oldAddress)
            {
                KMemoryInfo info = _owner.MemoryManager.QueryMemory(address);

                if (info.State == MemoryState.Reserved)
                {
                    break;
                }

                if (info.State == MemoryState.CodeStatic && info.Permission == KMemoryPermission.ReadAndExecute)
                {
                    LoadMod0Symbols(_owner.CpuMemory, info.Address, info.Size);
                }

                oldAddress = address;

                address = info.Address + info.Size;
            }
        }

        private void LoadMod0Symbols(IVirtualMemoryManager memory, ulong textOffset, ulong textSize)
        {
            ulong mod0Offset = textOffset + memory.Read<uint>(textOffset + 4);

            if (mod0Offset < textOffset || !memory.IsMapped(mod0Offset) || (mod0Offset & 3) != 0)
            {
                return;
            }

            Dictionary<ElfDynamicTag, ulong> dynamic = new();

            int mod0Magic = memory.Read<int>(mod0Offset + 0x0);

            if (mod0Magic != Mod0)
            {
                return;
            }

            ulong dynamicOffset = memory.Read<uint>(mod0Offset + 0x4) + mod0Offset;
            ulong bssStartOffset = memory.Read<uint>(mod0Offset + 0x8) + mod0Offset;
            ulong bssEndOffset = memory.Read<uint>(mod0Offset + 0xc) + mod0Offset;
            ulong ehHdrStartOffset = memory.Read<uint>(mod0Offset + 0x10) + mod0Offset;
            ulong ehHdrEndOffset = memory.Read<uint>(mod0Offset + 0x14) + mod0Offset;
            ulong modObjOffset = memory.Read<uint>(mod0Offset + 0x18) + mod0Offset;

            bool isAArch32 = memory.Read<ulong>(dynamicOffset) > 0xFFFFFFFF || memory.Read<ulong>(dynamicOffset + 0x10) > 0xFFFFFFFF;

            while (true)
            {
                ulong tagVal;
                ulong value;

                if (isAArch32)
                {
                    tagVal = memory.Read<uint>(dynamicOffset + 0);
                    value = memory.Read<uint>(dynamicOffset + 4);

                    dynamicOffset += 0x8;
                }
                else
                {
                    tagVal = memory.Read<ulong>(dynamicOffset + 0);
                    value = memory.Read<ulong>(dynamicOffset + 8);

                    dynamicOffset += 0x10;
                }

                ElfDynamicTag tag = (ElfDynamicTag)tagVal;

                if (tag == ElfDynamicTag.DT_NULL)
                {
                    break;
                }

                dynamic[tag] = value;
            }

            if (!dynamic.TryGetValue(ElfDynamicTag.DT_STRTAB, out ulong strTab) ||
                !dynamic.TryGetValue(ElfDynamicTag.DT_SYMTAB, out ulong symTab) ||
                !dynamic.TryGetValue(ElfDynamicTag.DT_SYMENT, out ulong symEntSize))
            {
                return;
            }

            ulong strTblAddr = textOffset + strTab;
            ulong symTblAddr = textOffset + symTab;

            List<ElfSymbol> symbols = [];

            while (symTblAddr < strTblAddr)
            {
                ElfSymbol sym = isAArch32 ? GetSymbol32(memory, symTblAddr, strTblAddr) : GetSymbol64(memory, symTblAddr, strTblAddr);

                symbols.Add(sym);

                symTblAddr += symEntSize;
            }

            lock (_images)
            {
                _images.Add(new Image(textOffset, textSize, symbols.OrderBy(x => x.Value).ToArray()));
            }
        }

        private static ElfSymbol GetSymbol64(IVirtualMemoryManager memory, ulong address, ulong strTblAddr)
        {
            ElfSymbol64 sym = memory.Read<ElfSymbol64>(address);

            uint nameIndex = sym.NameOffset;

            StringBuilder nameBuilder = new();

            for (int chr; (chr = memory.Read<byte>(strTblAddr + nameIndex++)) != 0;)
            {
                nameBuilder.Append((char)chr);
            }

            return new ElfSymbol(nameBuilder.ToString(), sym.Info, sym.Other, sym.SectionIndex, sym.ValueAddress, sym.Size);
        }

        private static ElfSymbol GetSymbol32(IVirtualMemoryManager memory, ulong address, ulong strTblAddr)
        {
            ElfSymbol32 sym = memory.Read<ElfSymbol32>(address);

            uint nameIndex = sym.NameOffset;

            StringBuilder nameBuilder = new();

            for (int chr; (chr = memory.Read<byte>(strTblAddr + nameIndex++)) != 0;)
            {
                nameBuilder.Append((char)chr);
            }

            return new ElfSymbol(nameBuilder.ToString(), sym.Info, sym.Other, sym.SectionIndex, sym.ValueAddress, sym.Size);
        }
    }
}
