using ARMeilleure.Memory;
using Ryujinx.Cpu.LightningJit.CodeGen.Arm64;
using System;
using System.Collections.Generic;

namespace Ryujinx.Cpu.LightningJit.Arm32
{
    class CodeGenContext
    {
        public CodeWriter CodeWriter { get; }
        public Assembler Arm64Assembler { get; }
        public RegisterAllocator RegisterAllocator { get; }

        public MemoryManagerType MemoryManagerType { get; }

        private uint _instructionAddress;

        public bool IsThumb { get; }
        public uint Pc { get; private set; }
        public bool InITBlock { get; private set; }

        private InstInfo _nextInstruction;
        private bool _skipNextInstruction;

        private readonly ArmCondition[] _itConditions;
        private int _itCount;

        private readonly List<PendingBranch> _pendingBranches;

        private bool _nzcvModified;

        public CodeGenContext(CodeWriter codeWriter, Assembler arm64Assembler, RegisterAllocator registerAllocator, MemoryManagerType mmType, bool isThumb)
        {
            CodeWriter = codeWriter;
            Arm64Assembler = arm64Assembler;
            RegisterAllocator = registerAllocator;
            MemoryManagerType = mmType;
            _itConditions = new ArmCondition[4];
            _pendingBranches = [];
            IsThumb = isThumb;
        }

        public void SetPc(uint address)
        {
            // Due to historical reasons, the PC value is always 2 instructions ahead on 32-bit Arm CPUs.
            Pc = address + (IsThumb ? 4u : 8u);
            _instructionAddress = address;
        }

        public void SetNextInstruction(InstInfo info)
        {
            _nextInstruction = info;
        }

        public InstInfo PeekNextInstruction()
        {
            return _nextInstruction;
        }

        public void SetSkipNextInstruction()
        {
            _skipNextInstruction = true;
        }

        public bool ConsumeSkipNextInstruction()
        {
            bool skip = _skipNextInstruction;
            _skipNextInstruction = false;

            return skip;
        }

        public void AddPendingBranch(InstName name, int offset)
        {
            _pendingBranches.Add(new(BranchType.Branch, Pc + (uint)offset, 0u, name, CodeWriter.InstructionPointer));
        }

        public void AddPendingCall(uint targetAddress, uint nextAddress)
        {
            _pendingBranches.Add(new(BranchType.Call, targetAddress, nextAddress, InstName.BlI, CodeWriter.InstructionPointer));

            RegisterAllocator.EnsureTempGprRegisters(1);
            RegisterAllocator.MarkGprAsUsed(RegisterUtils.LrRegister);
        }

        public void AddPendingIndirectBranch(InstName name, uint targetRegister)
        {
            _pendingBranches.Add(new(BranchType.IndirectBranch, targetRegister, 0u, name, CodeWriter.InstructionPointer));

            RegisterAllocator.MarkGprAsUsed((int)targetRegister);
        }

        public void AddPendingTableBranch(uint rn, uint rm, bool halfword)
        {
            _pendingBranches.Add(new(halfword ? BranchType.TableBranchHalfword : BranchType.TableBranchByte, rn, rm, InstName.Tbb, CodeWriter.InstructionPointer));

            RegisterAllocator.EnsureTempGprRegisters(2);
            RegisterAllocator.MarkGprAsUsed((int)rn);
            RegisterAllocator.MarkGprAsUsed((int)rm);
        }

        public void AddPendingIndirectCall(uint targetRegister, uint nextAddress)
        {
            _pendingBranches.Add(new(BranchType.IndirectCall, targetRegister, nextAddress, InstName.BlxR, CodeWriter.InstructionPointer));

            RegisterAllocator.EnsureTempGprRegisters(targetRegister == RegisterUtils.LrRegister ? 1 : 0);
            RegisterAllocator.MarkGprAsUsed((int)targetRegister);
            RegisterAllocator.MarkGprAsUsed(RegisterUtils.LrRegister);
        }

        public void AddPendingSyncPoint()
        {
            _pendingBranches.Add(new(BranchType.SyncPoint, 0, 0, default, CodeWriter.InstructionPointer));

            RegisterAllocator.EnsureTempGprRegisters(1);
        }

        public void AddPendingBkpt(uint imm)
        {
            _pendingBranches.Add(new(BranchType.SoftwareInterrupt, imm, _instructionAddress, InstName.Bkpt, CodeWriter.InstructionPointer));

            RegisterAllocator.EnsureTempGprRegisters(1);
        }

        public void AddPendingSvc(uint imm)
        {
            _pendingBranches.Add(new(BranchType.SoftwareInterrupt, imm, _instructionAddress, InstName.Svc, CodeWriter.InstructionPointer));

            RegisterAllocator.EnsureTempGprRegisters(1);
        }

        public void AddPendingUdf(uint imm)
        {
            _pendingBranches.Add(new(BranchType.SoftwareInterrupt, imm, _instructionAddress, InstName.Udf, CodeWriter.InstructionPointer));

            RegisterAllocator.EnsureTempGprRegisters(1);
        }

        public void AddPendingReadCntpct(uint rt, uint rt2)
        {
            _pendingBranches.Add(new(BranchType.ReadCntpct, rt, rt2, InstName.Mrrc, CodeWriter.InstructionPointer));

            RegisterAllocator.EnsureTempGprRegisters(1);
        }

        public IEnumerable<PendingBranch> GetPendingBranches()
        {
            return _pendingBranches;
        }

        public void SetItBlockStart(ReadOnlySpan<ArmCondition> conditions)
        {
            _itCount = conditions.Length;

            for (int index = 0; index < conditions.Length; index++)
            {
                _itConditions[index] = conditions[index];
            }

            InITBlock = true;
        }

        public bool ConsumeItCondition(out ArmCondition condition)
        {
            if (_itCount != 0)
            {
                condition = _itConditions[--_itCount];

                return true;
            }

            condition = ArmCondition.Al;

            return false;
        }

        public void UpdateItState()
        {
            if (_itCount == 0)
            {
                InITBlock = false;
            }
        }

        public void SetNzcvModified()
        {
            _nzcvModified = true;
        }

        public bool ConsumeNzcvModified()
        {
            bool modified = _nzcvModified;
            _nzcvModified = false;

            return modified;
        }
    }
}
