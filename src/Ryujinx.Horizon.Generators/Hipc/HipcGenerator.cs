using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Horizon.Generators.Hipc
{
    [Generator]
    class HipcGenerator : ISourceGenerator
    {
        private const string ArgVariablePrefix = "arg";
        private const string ResultVariableName = "result";
        private const string IsBufferMapAliasVariableName = "isBufferMapAlias";
        private const string InObjectsVariableName = "inObjects";
        private const string OutObjectsVariableName = "outObjects";
        private const string ResponseVariableName = "response";
        private const string OutRawDataVariableName = "outRawData";

        private const string TypeSystemBuffersReadOnlySequence = "System.Buffers.ReadOnlySequence";
        private const string TypeSystemMemory = "System.Memory";
        private const string TypeSystemReadOnlySpan = "System.ReadOnlySpan";
        private const string TypeSystemSpan = "System.Span";
        private const string TypeStructLayoutAttribute = "System.Runtime.InteropServices.StructLayoutAttribute";

        public const string CommandAttributeName = "CmifCommandAttribute";

        private const string TypeResult = "Ryujinx.Horizon.Common.Result";
        private const string TypeBufferAttribute = "Ryujinx.Horizon.Sdk.Sf.BufferAttribute";
        private const string TypeCopyHandleAttribute = "Ryujinx.Horizon.Sdk.Sf.CopyHandleAttribute";
        private const string TypeMoveHandleAttribute = "Ryujinx.Horizon.Sdk.Sf.MoveHandleAttribute";
        private const string TypeClientProcessIdAttribute = "Ryujinx.Horizon.Sdk.Sf.ClientProcessIdAttribute";
        private const string TypeCommandAttribute = "Ryujinx.Horizon.Sdk.Sf." + CommandAttributeName;
        private const string TypeIServiceObject = "Ryujinx.Horizon.Sdk.Sf.IServiceObject";

        private enum Modifier
        {
            None,
            Ref,
            Out,
            In,
        }

        private readonly struct OutParameter
        {
            public readonly string Name;
            public readonly string TypeName;
            public readonly int Index;
            public readonly CommandArgType Type;

            public OutParameter(string name, string typeName, int index, CommandArgType type)
            {
                Name = name;
                TypeName = typeName;
                Index = index;
                Type = type;
            }
        }

        public void Execute(GeneratorExecutionContext context)
        {
            HipcSyntaxReceiver syntaxReceiver = (HipcSyntaxReceiver)context.SyntaxReceiver;

            foreach (CommandInterface commandInterface in syntaxReceiver.CommandInterfaces)
            {
                if (!NeedsIServiceObjectImplementation(context.Compilation, commandInterface.ClassDeclarationSyntax))
                {
                    continue;
                }

                CodeGenerator generator = new();
                string className = commandInterface.ClassDeclarationSyntax.Identifier.ToString();

                generator.AppendLine("using Ryujinx.Horizon.Common;");
                generator.AppendLine("using Ryujinx.Horizon.Sdk.Sf;");
                generator.AppendLine("using Ryujinx.Horizon.Sdk.Sf.Cmif;");
                generator.AppendLine("using Ryujinx.Horizon.Sdk.Sf.Hipc;");
                generator.AppendLine("using System;");
                generator.AppendLine("using System.Collections.Frozen;");
                generator.AppendLine("using System.Collections.Generic;");
                generator.AppendLine("using System.Runtime.CompilerServices;");
                generator.AppendLine("using System.Runtime.InteropServices;");
                generator.AppendLine();
                generator.EnterScope($"namespace {GetNamespaceName(commandInterface.ClassDeclarationSyntax)}");
                generator.EnterScope($"partial class {className}");

                GenerateMethodTable(generator, context.Compilation, commandInterface);

                foreach (MethodDeclarationSyntax method in commandInterface.CommandImplementations)
                {
                    generator.AppendLine();

                    GenerateMethod(generator, context.Compilation, method);
                }

                generator.LeaveScope();
                generator.LeaveScope();

                context.AddSource($"{GetNamespaceName(commandInterface.ClassDeclarationSyntax)}.{className}.g.cs", generator.ToString());
            }
        }

        private static string GetNamespaceName(SyntaxNode syntaxNode)
        {
            while (syntaxNode != null && !(syntaxNode is NamespaceDeclarationSyntax))
            {
                syntaxNode = syntaxNode.Parent;
            }

            if (syntaxNode == null)
            {
                return string.Empty;
            }

            return ((NamespaceDeclarationSyntax)syntaxNode).Name.ToString();
        }

        private static void GenerateMethodTable(CodeGenerator generator, Compilation compilation, CommandInterface commandInterface)
        {
            generator.EnterScope($"public IReadOnlyDictionary<int, CommandHandler> GetCommandHandlers()");

            if (commandInterface.CommandImplementations.Count == 0)
            {
                generator.AppendLine("return FrozenDictionary<int, CommandHandler>.Empty;");
            }
            else
            {
                generator.EnterScope($"return FrozenDictionary.ToFrozenDictionary(new []");

                foreach (MethodDeclarationSyntax method in commandInterface.CommandImplementations)
                {
                    foreach (string commandId in GetAttributeArguments(compilation, method, TypeCommandAttribute, 0))
                    {
                        string[] args = new string[method.ParameterList.Parameters.Count];

                        if (args.Length == 0)
                        {
                            generator.AppendLine($"KeyValuePair.Create({commandId}, new CommandHandler({method.Identifier.Text}, Array.Empty<CommandArg>())),");
                        }
                        else
                        {
                            int index = 0;

                            foreach (ParameterSyntax parameter in method.ParameterList.Parameters)
                            {
                                string canonicalTypeName = GetCanonicalTypeNameWithGenericArguments(compilation, parameter.Type);
                                CommandArgType argType = GetCommandArgType(compilation, parameter);

                                string arg;

                                if (argType == CommandArgType.Buffer)
                                {
                                    string bufferFlags = GetFirstAttributeArgument(compilation, parameter, TypeBufferAttribute, 0);
                                    string bufferFixedSize = GetFirstAttributeArgument(compilation, parameter, TypeBufferAttribute, 1);

                                    if (bufferFixedSize != null)
                                    {
                                        arg = $"new CommandArg({bufferFlags} | HipcBufferFlags.FixedSize, {bufferFixedSize})";
                                    }
                                    else
                                    {
                                        arg = $"new CommandArg({bufferFlags})";
                                    }
                                }
                                else if (argType == CommandArgType.InArgument || argType == CommandArgType.OutArgument)
                                {
                                    string alignment = GetTypeAlignmentExpression(compilation, parameter.Type);

                                    arg = $"new CommandArg(CommandArgType.{argType}, Unsafe.SizeOf<{canonicalTypeName}>(), {alignment})";
                                }
                                else
                                {
                                    arg = $"new CommandArg(CommandArgType.{argType})";
                                }

                                args[index++] = arg;
                            }

                            generator.AppendLine($"KeyValuePair.Create({commandId}, new CommandHandler({method.Identifier.Text}, {string.Join(", ", args)})),");
                        }
                    }
                }

                generator.LeaveScope(");");
            }

            generator.LeaveScope();
        }

        private static IEnumerable<string> GetAttributeArguments(Compilation compilation, SyntaxNode syntaxNode, string attributeName, int argIndex)
        {
            ISymbol symbol = compilation.GetSemanticModel(syntaxNode.SyntaxTree).GetDeclaredSymbol(syntaxNode);

            foreach (AttributeData attribute in symbol.GetAttributes())
            {
                if (attribute.AttributeClass.ToDisplayString() == attributeName && (uint)argIndex < (uint)attribute.ConstructorArguments.Length)
                {
                    yield return attribute.ConstructorArguments[argIndex].ToCSharpString();
                }
            }
        }

        private static string GetFirstAttributeArgument(Compilation compilation, SyntaxNode syntaxNode, string attributeName, int argIndex)
        {
            return GetAttributeArguments(compilation, syntaxNode, attributeName, argIndex).FirstOrDefault();
        }

        private static void GenerateMethod(CodeGenerator generator, Compilation compilation, MethodDeclarationSyntax method)
        {
            int inObjectsCount = 0;
            int outObjectsCount = 0;
            int buffersCount = 0;

            foreach (ParameterSyntax parameter in method.ParameterList.Parameters)
            {
                if (IsObject(compilation, parameter))
                {
                    if (IsIn(parameter))
                    {
                        inObjectsCount++;
                    }
                    else
                    {
                        outObjectsCount++;
                    }
                }
                else if (IsBuffer(compilation, parameter))
                {
                    buffersCount++;
                }
            }

            generator.EnterScope($"private Result {method.Identifier.Text}(" +
                "ref ServiceDispatchContext context, " +
                "HipcCommandProcessor processor, " +
                "ServerMessageRuntimeMetadata runtimeMetadata, " +
                "ReadOnlySpan<byte> inRawData, " +
                "ref Span<CmifOutHeader> outHeader)");

            bool returnsResult = method.ReturnType != null && GetCanonicalTypeName(compilation, method.ReturnType) == TypeResult;

            if (returnsResult || buffersCount != 0 || inObjectsCount != 0)
            {
                generator.AppendLine($"Result {ResultVariableName};");

                if (buffersCount != 0)
                {
                    generator.AppendLine($"Span<bool> {IsBufferMapAliasVariableName} = stackalloc bool[{method.ParameterList.Parameters.Count}];");
                    generator.AppendLine();

                    generator.AppendLine($"{ResultVariableName} = processor.ProcessBuffers(ref context, {IsBufferMapAliasVariableName}, runtimeMetadata);");
                    generator.EnterScope($"if ({ResultVariableName}.IsFailure)");
                    generator.AppendLine($"return {ResultVariableName};");
                    generator.LeaveScope();
                }

                generator.AppendLine();
            }

            List<OutParameter> outParameters = [];

            string[] args = new string[method.ParameterList.Parameters.Count];

            if (inObjectsCount != 0)
            {
                generator.AppendLine($"var {InObjectsVariableName} = new IServiceObject[{inObjectsCount}];");
                generator.AppendLine();

                generator.AppendLine($"{ResultVariableName} = processor.GetInObjects(context.Processor, {InObjectsVariableName});");
                generator.EnterScope($"if ({ResultVariableName}.IsFailure)");
                generator.AppendLine($"return {ResultVariableName};");
                generator.LeaveScope();
                generator.AppendLine();
            }

            if (outObjectsCount != 0)
            {
                generator.AppendLine($"var {OutObjectsVariableName} = new IServiceObject[{outObjectsCount}];");
            }

            int index = 0;
            int inArgIndex = 0;
            int outArgIndex = 0;
            int inCopyHandleIndex = 0;
            int inMoveHandleIndex = 0;
            int inObjectIndex = 0;

            foreach (ParameterSyntax parameter in method.ParameterList.Parameters)
            {
                string name = parameter.Identifier.Text;
                string argName = GetPrefixedArgName(name);
                string canonicalTypeName = GetCanonicalTypeNameWithGenericArguments(compilation, parameter.Type);
                CommandArgType argType = GetCommandArgType(compilation, parameter);
                Modifier modifier = GetModifier(parameter);
                bool isNonSpanBuffer = false;

                if (modifier == Modifier.Out)
                {
                    if (IsNonSpanOutBuffer(compilation, parameter))
                    {
                        generator.AppendLine($"using var {argName} = CommandSerialization.GetWritableRegion(processor.GetBufferRange({index}));");

                        argName = $"out {GenerateSpanCastElement0(canonicalTypeName, $"{argName}.Memory.Span")}";
                    }
                    else
                    {
                        outParameters.Add(new OutParameter(argName, canonicalTypeName, outArgIndex++, argType));

                        argName = $"out {canonicalTypeName} {argName}";
                    }
                }
                else
                {
                    string value = $"default({canonicalTypeName})";

                    switch (argType)
                    {
                        case CommandArgType.InArgument:
                            value = $"CommandSerialization.DeserializeArg<{canonicalTypeName}>(inRawData, processor.GetInArgOffset({inArgIndex++}))";
                            break;
                        case CommandArgType.InCopyHandle:
                            value = $"CommandSerialization.DeserializeCopyHandle(ref context, {inCopyHandleIndex++})";
                            break;
                        case CommandArgType.InMoveHandle:
                            value = $"CommandSerialization.DeserializeMoveHandle(ref context, {inMoveHandleIndex++})";
                            break;
                        case CommandArgType.ProcessId:
                            value = "CommandSerialization.DeserializeClientProcessId(ref context)";
                            break;
                        case CommandArgType.InObject:
                            value = $"{InObjectsVariableName}[{inObjectIndex++}]";
                            break;
                        case CommandArgType.Buffer:
                            if (IsMemory(compilation, parameter))
                            {
                                value = $"CommandSerialization.GetWritableRegion(processor.GetBufferRange({index}))";
                            }
                            else if (IsReadOnlySequence(compilation, parameter))
                            {
                                value = $"CommandSerialization.GetReadOnlySequence(processor.GetBufferRange({index}))";
                            }
                            else if (IsReadOnlySpan(compilation, parameter))
                            {
                                string spanGenericTypeName = GetCanonicalTypeNameOfGenericArgument(compilation, parameter.Type, 0);
                                value = GenerateSpanCast(spanGenericTypeName, $"CommandSerialization.GetReadOnlySpan(processor.GetBufferRange({index}))");
                            }
                            else if (IsSpan(compilation, parameter))
                            {
                                value = $"CommandSerialization.GetWritableRegion(processor.GetBufferRange({index}))";
                            }
                            else
                            {
                                value = $"CommandSerialization.GetRef<{canonicalTypeName}>(processor.GetBufferRange({index}))";
                                isNonSpanBuffer = true;
                            }
                            break;
                    }

                    if (IsMemory(compilation, parameter))
                    {
                        generator.AppendLine($"using var {argName} = {value};");

                        argName = $"{argName}.Memory";
                    }
                    else if (IsSpan(compilation, parameter))
                    {
                        generator.AppendLine($"using var {argName} = {value};");

                        string spanGenericTypeName = GetCanonicalTypeNameOfGenericArgument(compilation, parameter.Type, 0);
                        argName = GenerateSpanCast(spanGenericTypeName, $"{argName}.Memory.Span");
                    }
                    else if (isNonSpanBuffer)
                    {
                        generator.AppendLine($"ref var {argName} = ref {value};");
                    }
                    else if (argType == CommandArgType.InObject)
                    {
                        generator.EnterScope($"if (!({value} is {canonicalTypeName} {argName}))");
                        generator.AppendLine("return SfResult.InvalidInObject;");
                        generator.LeaveScope();
                    }
                    else
                    {
                        generator.AppendLine($"var {argName} = {value};");
                    }
                }

                if (modifier == Modifier.Ref)
                {
                    argName = $"ref {argName}";
                }
                else if (modifier == Modifier.In)
                {
                    argName = $"in {argName}";
                }

                args[index++] = argName;
            }

            if (args.Length - outParameters.Count > 0)
            {
                generator.AppendLine();
            }

            if (returnsResult)
            {
                generator.AppendLine($"{ResultVariableName} = {method.Identifier.Text}({string.Join(", ", args)});");
                generator.AppendLine();

                generator.AppendLine($"Span<byte> {OutRawDataVariableName};");
                generator.AppendLine();

                generator.EnterScope($"if ({ResultVariableName}.IsFailure)");
                generator.AppendLine($"context.Processor.PrepareForErrorReply(ref context, out {OutRawDataVariableName}, runtimeMetadata);");
                generator.AppendLine($"CommandHandler.GetCmifOutHeaderPointer(ref outHeader, ref {OutRawDataVariableName});");
                generator.AppendLine($"return {ResultVariableName};");
                generator.LeaveScope();
            }
            else
            {
                generator.AppendLine($"{method.Identifier.Text}({string.Join(", ", args)});");

                generator.AppendLine();
                generator.AppendLine($"Span<byte> {OutRawDataVariableName};");
            }

            generator.AppendLine();

            generator.AppendLine($"var {ResponseVariableName} = context.Processor.PrepareForReply(ref context, out {OutRawDataVariableName}, runtimeMetadata);");
            generator.AppendLine($"CommandHandler.GetCmifOutHeaderPointer(ref outHeader, ref {OutRawDataVariableName});");
            generator.AppendLine();

            generator.EnterScope($"if ({OutRawDataVariableName}.Length < processor.OutRawDataSize)");
            generator.AppendLine("return SfResult.InvalidOutRawSize;");
            generator.LeaveScope();

            if (outParameters.Count != 0)
            {
                generator.AppendLine();

                int outCopyHandleIndex = 0;
                int outMoveHandleIndex = outObjectsCount;
                int outObjectIndex = 0;

                for (int outIndex = 0; outIndex < outParameters.Count; outIndex++)
                {
                    OutParameter outParameter = outParameters[outIndex];

                    switch (outParameter.Type)
                    {
                        case CommandArgType.OutArgument:
                            generator.AppendLine($"CommandSerialization.SerializeArg<{outParameter.TypeName}>({OutRawDataVariableName}, processor.GetOutArgOffset({outParameter.Index}), {outParameter.Name});");
                            break;
                        case CommandArgType.OutCopyHandle:
                            generator.AppendLine($"CommandSerialization.SerializeCopyHandle({ResponseVariableName}, {outCopyHandleIndex++}, {outParameter.Name});");
                            break;
                        case CommandArgType.OutMoveHandle:
                            generator.AppendLine($"CommandSerialization.SerializeMoveHandle({ResponseVariableName}, {outMoveHandleIndex++}, {outParameter.Name});");
                            break;
                        case CommandArgType.OutObject:
                            generator.AppendLine($"{OutObjectsVariableName}[{outObjectIndex++}] = {outParameter.Name};");
                            break;
                    }
                }
            }

            generator.AppendLine();

            if (outObjectsCount != 0 || buffersCount != 0)
            {
                if (outObjectsCount != 0)
                {
                    generator.AppendLine($"processor.SetOutObjects(ref context, {ResponseVariableName}, {OutObjectsVariableName});");
                }

                if (buffersCount != 0)
                {
                    generator.AppendLine($"processor.SetOutBuffers({ResponseVariableName}, {IsBufferMapAliasVariableName});");
                }

                generator.AppendLine();
            }

            generator.AppendLine("return Result.Success;");
            generator.LeaveScope();
        }

        private static string GetPrefixedArgName(string name)
        {
            return ArgVariablePrefix + name[0].ToString().ToUpperInvariant() + name.Substring(1);
        }

        private static string GetCanonicalTypeNameOfGenericArgument(Compilation compilation, SyntaxNode syntaxNode, int argIndex)
        {
            if (syntaxNode is GenericNameSyntax genericNameSyntax)
            {
                if ((uint)argIndex < (uint)genericNameSyntax.TypeArgumentList.Arguments.Count)
                {
                    return GetCanonicalTypeNameWithGenericArguments(compilation, genericNameSyntax.TypeArgumentList.Arguments[argIndex]);
                }
            }

            return GetCanonicalTypeName(compilation, syntaxNode);
        }

        private static string GetCanonicalTypeNameWithGenericArguments(Compilation compilation, SyntaxNode syntaxNode)
        {
            TypeInfo typeInfo = compilation.GetSemanticModel(syntaxNode.SyntaxTree).GetTypeInfo(syntaxNode);

            return typeInfo.Type.ToDisplayString();
        }

        private static string GetCanonicalTypeName(Compilation compilation, SyntaxNode syntaxNode)
        {
            TypeInfo typeInfo = compilation.GetSemanticModel(syntaxNode.SyntaxTree).GetTypeInfo(syntaxNode);
            string typeName = typeInfo.Type.ToDisplayString();

            int genericArgsStartIndex = typeName.IndexOf('<');
            if (genericArgsStartIndex >= 0)
            {
                return typeName.Substring(0, genericArgsStartIndex);
            }

            return typeName;
        }

        private static SpecialType GetSpecialTypeName(Compilation compilation, SyntaxNode syntaxNode)
        {
            TypeInfo typeInfo = compilation.GetSemanticModel(syntaxNode.SyntaxTree).GetTypeInfo(syntaxNode);

            return typeInfo.Type.SpecialType;
        }

        private static string GetTypeAlignmentExpression(Compilation compilation, SyntaxNode syntaxNode)
        {
            TypeInfo typeInfo = compilation.GetSemanticModel(syntaxNode.SyntaxTree).GetTypeInfo(syntaxNode);

            // Since there's no way to get the alignment for a arbitrary type here, let's assume that all
            // "special" types are primitive types aligned to their own length.
            // Otherwise, assume that the type is a custom struct, that either defines an explicit alignment
            // or has an alignment of 1 which is the lowest possible value.
            if (typeInfo.Type.SpecialType == SpecialType.None)
            {
                string pack = GetTypeFirstNamedAttributeAgument(compilation, syntaxNode, TypeStructLayoutAttribute, "Pack");

                return pack ?? "1";
            }
            else
            {
                return $"Unsafe.SizeOf<{typeInfo.Type.ToDisplayString()}>()";
            }
        }

        private static string GetTypeFirstNamedAttributeAgument(Compilation compilation, SyntaxNode syntaxNode, string attributeName, string argName)
        {
            ISymbol symbol = compilation.GetSemanticModel(syntaxNode.SyntaxTree).GetTypeInfo(syntaxNode).Type;

            foreach (AttributeData attribute in symbol.GetAttributes())
            {
                if (attribute.AttributeClass.ToDisplayString() == attributeName)
                {
                    foreach (KeyValuePair<string, TypedConstant> kv in attribute.NamedArguments)
                    {
                        if (kv.Key == argName)
                        {
                            return kv.Value.ToCSharpString();
                        }
                    }
                }
            }

            return null;
        }

        private static CommandArgType GetCommandArgType(Compilation compilation, ParameterSyntax parameter)
        {
            CommandArgType type = CommandArgType.Invalid;

            if (IsIn(parameter))
            {
                if (IsArgument(compilation, parameter))
                {
                    type = CommandArgType.InArgument;
                }
                else if (IsBuffer(compilation, parameter))
                {
                    type = CommandArgType.Buffer;
                }
                else if (IsCopyHandle(compilation, parameter))
                {
                    type = CommandArgType.InCopyHandle;
                }
                else if (IsMoveHandle(compilation, parameter))
                {
                    type = CommandArgType.InMoveHandle;
                }
                else if (IsObject(compilation, parameter))
                {
                    type = CommandArgType.InObject;
                }
                else if (IsProcessId(compilation, parameter))
                {
                    type = CommandArgType.ProcessId;
                }
            }
            else if (IsOut(parameter))
            {
                if (IsArgument(compilation, parameter))
                {
                    type = CommandArgType.OutArgument;
                }
                else if (IsNonSpanOutBuffer(compilation, parameter))
                {
                    type = CommandArgType.Buffer;
                }
                else if (IsCopyHandle(compilation, parameter))
                {
                    type = CommandArgType.OutCopyHandle;
                }
                else if (IsMoveHandle(compilation, parameter))
                {
                    type = CommandArgType.OutMoveHandle;
                }
                else if (IsObject(compilation, parameter))
                {
                    type = CommandArgType.OutObject;
                }
            }

            return type;
        }

        private static bool IsArgument(Compilation compilation, ParameterSyntax parameter)
        {
            return !IsBuffer(compilation, parameter) &&
                   !IsHandle(compilation, parameter) &&
                   !IsObject(compilation, parameter) &&
                   !IsProcessId(compilation, parameter) &&
                   IsUnmanagedType(compilation, parameter.Type);
        }

        private static bool IsBuffer(Compilation compilation, ParameterSyntax parameter)
        {
            return HasAttribute(compilation, parameter, TypeBufferAttribute) &&
                   IsValidTypeForBuffer(compilation, parameter);
        }

        private static bool IsNonSpanOutBuffer(Compilation compilation, ParameterSyntax parameter)
        {
            return HasAttribute(compilation, parameter, TypeBufferAttribute) &&
                   IsUnmanagedType(compilation, parameter.Type);
        }

        private static bool IsValidTypeForBuffer(Compilation compilation, ParameterSyntax parameter)
        {
            return IsMemory(compilation, parameter) ||
                   IsReadOnlySequence(compilation, parameter) ||
                   IsReadOnlySpan(compilation, parameter) ||
                   IsSpan(compilation, parameter) ||
                   IsUnmanagedType(compilation, parameter.Type);
        }

        private static bool IsUnmanagedType(Compilation compilation, SyntaxNode syntaxNode)
        {
            TypeInfo typeInfo = compilation.GetSemanticModel(syntaxNode.SyntaxTree).GetTypeInfo(syntaxNode);

            return typeInfo.Type.IsUnmanagedType;
        }

        private static bool IsMemory(Compilation compilation, ParameterSyntax parameter)
        {
            return GetCanonicalTypeName(compilation, parameter.Type) == TypeSystemMemory;
        }

        private static bool IsReadOnlySequence(Compilation compilation, ParameterSyntax parameter)
        {
            return GetCanonicalTypeName(compilation, parameter.Type) == TypeSystemBuffersReadOnlySequence;
        }

        private static bool IsReadOnlySpan(Compilation compilation, ParameterSyntax parameter)
        {
            return GetCanonicalTypeName(compilation, parameter.Type) == TypeSystemReadOnlySpan;
        }

        private static bool IsSpan(Compilation compilation, ParameterSyntax parameter)
        {
            return GetCanonicalTypeName(compilation, parameter.Type) == TypeSystemSpan;
        }

        private static bool IsHandle(Compilation compilation, ParameterSyntax parameter)
        {
            return IsCopyHandle(compilation, parameter) || IsMoveHandle(compilation, parameter);
        }

        private static bool IsCopyHandle(Compilation compilation, ParameterSyntax parameter)
        {
            return HasAttribute(compilation, parameter, TypeCopyHandleAttribute) &&
                   GetSpecialTypeName(compilation, parameter.Type) == SpecialType.System_Int32;
        }

        private static bool IsMoveHandle(Compilation compilation, ParameterSyntax parameter)
        {
            return HasAttribute(compilation, parameter, TypeMoveHandleAttribute) &&
                   GetSpecialTypeName(compilation, parameter.Type) == SpecialType.System_Int32;
        }

        private static bool IsObject(Compilation compilation, ParameterSyntax parameter)
        {
            SyntaxNode syntaxNode = parameter.Type;
            TypeInfo typeInfo = compilation.GetSemanticModel(syntaxNode.SyntaxTree).GetTypeInfo(syntaxNode);

            return typeInfo.Type.ToDisplayString() == TypeIServiceObject ||
                   typeInfo.Type.AllInterfaces.Any(x => x.ToDisplayString() == TypeIServiceObject);
        }

        private static bool IsProcessId(Compilation compilation, ParameterSyntax parameter)
        {
            return HasAttribute(compilation, parameter, TypeClientProcessIdAttribute) &&
                   GetSpecialTypeName(compilation, parameter.Type) == SpecialType.System_UInt64;
        }

        private static bool IsIn(ParameterSyntax parameter)
        {
            return !IsOut(parameter);
        }

        private static bool IsOut(ParameterSyntax parameter)
        {
            return parameter.Modifiers.Any(SyntaxKind.OutKeyword);
        }

        private static Modifier GetModifier(ParameterSyntax parameter)
        {
            foreach (SyntaxToken syntaxToken in parameter.Modifiers)
            {
                if (syntaxToken.IsKind(SyntaxKind.RefKeyword))
                {
                    return Modifier.Ref;
                }
                else if (syntaxToken.IsKind(SyntaxKind.OutKeyword))
                {
                    return Modifier.Out;
                }
                else if (syntaxToken.IsKind(SyntaxKind.InKeyword))
                {
                    return Modifier.In;
                }
            }

            return Modifier.None;
        }

        private static string GenerateSpanCastElement0(string targetType, string input)
        {
            return $"{GenerateSpanCast(targetType, input)}[0]";
        }

        private static string GenerateSpanCast(string targetType, string input)
        {
            return targetType == "byte"
                ? input
                : $"MemoryMarshal.Cast<byte, {targetType}>({input})";
        }

        private static bool HasAttribute(Compilation compilation, ParameterSyntax parameterSyntax, string fullAttributeName)
        {
            foreach (AttributeListSyntax attributeList in parameterSyntax.AttributeLists)
            {
                foreach (AttributeSyntax attribute in attributeList.Attributes)
                {
                    if (GetCanonicalTypeName(compilation, attribute) == fullAttributeName)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool NeedsIServiceObjectImplementation(Compilation compilation, ClassDeclarationSyntax classDeclarationSyntax)
        {
            ITypeSymbol type = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree).GetDeclaredSymbol(classDeclarationSyntax);
            INamedTypeSymbol serviceObjectInterface = type.AllInterfaces.FirstOrDefault(x => x.ToDisplayString() == TypeIServiceObject);
            ISymbol interfaceMember = serviceObjectInterface?.GetMembers().FirstOrDefault(x => x.Name == "GetCommandHandlers");

            // Return true only if the class implements IServiceObject but does not actually implement the method
            // that the interface defines, since this is the only case we want to handle, if the method already exists
            // we have nothing to do.
            return serviceObjectInterface != null && type.FindImplementationForInterfaceMember(interfaceMember) == null;
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new HipcSyntaxReceiver());
        }
    }
}
