using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using static Spv.Specification;

namespace Spv.Generator
{
    public partial class Module
    {
        // TODO: Register on SPIR-V registry.
        private const int GeneratorId = 0;

        private readonly uint _version;

        private uint _bound;

        // Follow spec order here while keeping it as simple as possible.
        private readonly List<Capability> _capabilities;
        private readonly List<string> _extensions;
        private readonly Dictionary<DeterministicStringKey, Instruction> _extInstImports;
        private AddressingModel _addressingModel;
        private MemoryModel _memoryModel;

        private readonly List<Instruction> _entrypoints;
        private readonly List<Instruction> _executionModes;
        private readonly List<Instruction> _debug;
        private readonly List<Instruction> _annotations;

        // In the declaration block.
        private readonly Dictionary<TypeDeclarationKey, Instruction> _typeDeclarations;
        private readonly List<Instruction> _typeDeclarationsList;
        // In the declaration block.
        private readonly List<Instruction> _globals;
        // In the declaration block.
        private readonly Dictionary<ConstantKey, Instruction> _constants;
        // In the declaration block, for function that aren't defined in the module.
        private readonly List<Instruction> _functionsDeclarations;

        private readonly List<Instruction> _functionsDefinitions;

        private readonly GeneratorPool<Instruction> _instPool;
        private readonly GeneratorPool<LiteralInteger> _integerPool;

        public Module(uint version, GeneratorPool<Instruction> instPool = null, GeneratorPool<LiteralInteger> integerPool = null)
        {
            _version = version;
            _bound = 1;
            _capabilities = new List<Capability>();
            _extensions = new List<string>();
            _extInstImports = new Dictionary<DeterministicStringKey, Instruction>();
            _addressingModel = AddressingModel.Logical;
            _memoryModel = MemoryModel.Simple;
            _entrypoints = new List<Instruction>();
            _executionModes = new List<Instruction>();
            _debug = new List<Instruction>();
            _annotations = new List<Instruction>();
            _typeDeclarations = new Dictionary<TypeDeclarationKey, Instruction>();
            _typeDeclarationsList = new List<Instruction>();
            _constants = new Dictionary<ConstantKey, Instruction>();
            _globals = new List<Instruction>();
            _functionsDeclarations = new List<Instruction>();
            _functionsDefinitions = new List<Instruction>();

            _instPool = instPool ?? new GeneratorPool<Instruction>();
            _integerPool = integerPool ?? new GeneratorPool<LiteralInteger>();

            LiteralInteger.RegisterPool(_integerPool);
        }

        private uint GetNewId()
        {
            return _bound++;
        }

        public void AddCapability(Capability capability)
        {
            _capabilities.Add(capability);
        }

        public void AddExtension(string extension)
        {
            _extensions.Add(extension);
        }

        public Instruction NewInstruction(Op opcode, uint id = Instruction.InvalidId, Instruction resultType = null)
        {
            var result = _instPool.Allocate();
            result.Set(opcode, id, resultType);

            return result;
        }

        public Instruction AddExtInstImport(string import)
        {
            var key = new DeterministicStringKey(import);

            if (_extInstImports.TryGetValue(key, out Instruction extInstImport))
            {
                // Update the duplicate instance to use the good id so it ends up being encoded correctly.
                return extInstImport;
            }

            Instruction instruction = NewInstruction(Op.OpExtInstImport);
            instruction.AddOperand(import);

            instruction.SetId(GetNewId());

            _extInstImports.Add(key, instruction);

            return instruction;
        }

        private void AddTypeDeclaration(Instruction instruction, bool forceIdAllocation)
        {
            var key = new TypeDeclarationKey(instruction);

            if (!forceIdAllocation)
            {
                if (_typeDeclarations.TryGetValue(key, out Instruction typeDeclaration))
                {
                    // Update the duplicate instance to use the good id so it ends up being encoded correctly.

                    instruction.SetId(typeDeclaration.Id);

                    return;
                }
            }

            instruction.SetId(GetNewId());

            _typeDeclarations[key] = instruction;
            _typeDeclarationsList.Add(instruction);
        }

        public void AddEntryPoint(ExecutionModel executionModel, Instruction function, string name, params ReadOnlySpan<Instruction> interfaces)
        {
            Debug.Assert(function.Opcode == Op.OpFunction);

            Instruction entryPoint = NewInstruction(Op.OpEntryPoint);

            entryPoint.AddOperand(executionModel);
            entryPoint.AddOperand(function);
            entryPoint.AddOperand(name);
            entryPoint.AddOperand(interfaces);

            _entrypoints.Add(entryPoint);
        }

        public void AddExecutionMode(Instruction function, ExecutionMode mode, params ReadOnlySpan<IOperand> parameters)
        {
            Debug.Assert(function.Opcode == Op.OpFunction);

            Instruction executionModeInstruction = NewInstruction(Op.OpExecutionMode);

            executionModeInstruction.AddOperand(function);
            executionModeInstruction.AddOperand(mode);
            executionModeInstruction.AddOperand(parameters);

            _executionModes.Add(executionModeInstruction);
        }

        private void AddToFunctionDefinitions(Instruction instruction)
        {
            Debug.Assert(instruction.Opcode != Op.OpTypeInt);
            _functionsDefinitions.Add(instruction);
        }

        private void AddAnnotation(Instruction annotation)
        {
            _annotations.Add(annotation);
        }

        private void AddDebug(Instruction debug)
        {
            _debug.Add(debug);
        }

        public void AddLabel(Instruction label)
        {
            Debug.Assert(label.Opcode == Op.OpLabel);

            label.SetId(GetNewId());

            AddToFunctionDefinitions(label);
        }

        public void AddLocalVariable(Instruction variable)
        {
            // TODO: Ensure it has the local modifier.
            Debug.Assert(variable.Opcode == Op.OpVariable);

            variable.SetId(GetNewId());

            AddToFunctionDefinitions(variable);
        }

        public void AddGlobalVariable(Instruction variable)
        {
            // TODO: Ensure it has the global modifier.
            // TODO: All constants opcodes (OpSpecXXX and the rest of the OpConstantXXX).
            Debug.Assert(variable.Opcode == Op.OpVariable);

            variable.SetId(GetNewId());

            _globals.Add(variable);
        }

        private void AddConstant(Instruction constant)
        {
            Debug.Assert(constant.Opcode == Op.OpConstant ||
                         constant.Opcode == Op.OpConstantFalse ||
                         constant.Opcode == Op.OpConstantTrue ||
                         constant.Opcode == Op.OpConstantNull ||
                         constant.Opcode == Op.OpConstantComposite);

            var key = new ConstantKey(constant);

            if (_constants.TryGetValue(key, out Instruction global))
            {
                // Update the duplicate instance to use the good id so it ends up being encoded correctly.
                constant.SetId(global.Id);

                return;
            }

            constant.SetId(GetNewId());

            _constants.Add(key, constant);
        }

        public Instruction ExtInst(Instruction resultType, Instruction set, LiteralInteger instruction, params ReadOnlySpan<IOperand> parameters)
        {
            Instruction result = NewInstruction(Op.OpExtInst, GetNewId(), resultType);

            result.AddOperand(set);
            result.AddOperand(instruction);
            result.AddOperand(parameters);
            AddToFunctionDefinitions(result);

            return result;
        }

        public void SetMemoryModel(AddressingModel addressingModel, MemoryModel memoryModel)
        {
            _addressingModel = addressingModel;
            _memoryModel = memoryModel;
        }

        // TODO: Find a way to make the auto generate one used.
        public Instruction OpenClPrintf(Instruction resultType, Instruction format, params ReadOnlySpan<Instruction> additionalarguments)
        {
            Instruction result = NewInstruction(Op.OpExtInst, GetNewId(), resultType);

            result.AddOperand(AddExtInstImport("OpenCL.std"));
            result.AddOperand((LiteralInteger)184);
            result.AddOperand(format);
            result.AddOperand(additionalarguments);
            AddToFunctionDefinitions(result);

            return result;
        }

        public byte[] Generate()
        {
            // Estimate the size needed for the generated code, to avoid expanding the MemoryStream.
            int sizeEstimate = 1024 + _functionsDefinitions.Count * 32;

            using MemoryStream stream = new(sizeEstimate);

            BinaryWriter writer = new(stream, System.Text.Encoding.ASCII);

            // Header
            writer.Write(MagicNumber);
            writer.Write(_version);
            writer.Write(GeneratorId);
            writer.Write(_bound);
            writer.Write(0u);

            // 1.
            foreach (Capability capability in _capabilities)
            {
                Instruction capabilityInstruction = NewInstruction(Op.OpCapability);

                capabilityInstruction.AddOperand(capability);
                capabilityInstruction.Write(writer);
            }

            // 2.
            foreach (string extension in _extensions)
            {
                Instruction extensionInstruction = NewInstruction(Op.OpExtension);

                extensionInstruction.AddOperand(extension);
                extensionInstruction.Write(writer);
            }

            // 3.
            foreach (Instruction extInstImport in _extInstImports.Values)
            {
                extInstImport.Write(writer);
            }

            // 4.
            Instruction memoryModelInstruction = NewInstruction(Op.OpMemoryModel);
            memoryModelInstruction.AddOperand(_addressingModel);
            memoryModelInstruction.AddOperand(_memoryModel);
            memoryModelInstruction.Write(writer);

            // 5.
            foreach (Instruction entrypoint in _entrypoints)
            {
                entrypoint.Write(writer);
            }

            // 6.
            foreach (Instruction executionMode in _executionModes)
            {
                executionMode.Write(writer);
            }

            // 7.
            // TODO: Order debug information correctly.
            foreach (Instruction debug in _debug)
            {
                debug.Write(writer);
            }

            // 8.
            foreach (Instruction annotation in _annotations)
            {
                annotation.Write(writer);
            }

            // Ensure that everything is in the right order in the declarations section.
            List<Instruction> declarations = new();
            declarations.AddRange(_typeDeclarationsList);
            declarations.AddRange(_globals);
            declarations.AddRange(_constants.Values);
            declarations.Sort((Instruction x, Instruction y) => x.Id.CompareTo(y.Id));

            // 9.
            foreach (Instruction declaration in declarations)
            {
                declaration.Write(writer);
            }

            // 10.
            foreach (Instruction functionDeclaration in _functionsDeclarations)
            {
                functionDeclaration.Write(writer);
            }

            // 11.
            foreach (Instruction functionDefinition in _functionsDefinitions)
            {
                functionDefinition.Write(writer);
            }

            _instPool.Clear();
            _integerPool.Clear();

            LiteralInteger.UnregisterPool();

            return stream.ToArray();
        }
    }
}
