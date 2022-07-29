// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#pragma warning disable IDE0150 // Prefer 'null' check over type check - requires a higher C# language version

using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Stride.Core.AssemblyProcessor
{
    public static class APUtilities
    {
        private static MethodDebugInformation TryRead(MethodDefinition def)
        {
            try {
                return def?.Module?.SymbolReader?.Read(def);
            } catch (System.ArgumentNullException) { // 60>System.ArgumentNullException: Value cannot be null. 60>Parameter name: document 60>   at Mono.Cecil.Cil.SequencePoint..ctor(Int32 offset, Document document) (...) 60>Unexpected error
            } catch (System.IndexOutOfRangeException) { // 1>System.IndexOutOfRangeException: Index was outside the bounds of the array.  1>   at Mono.Cecil.PE.ByteBuffer.ReadCompressedUInt32() (...) 1>Unexpected error
            }
            return null;
        }
        private static SequencePoint FindSequencePoint(MethodReference method, Instruction instruction, bool useAnySequencePoint)
        {
            var methoddef = method.IsDefinition ? (MethodDefinition)method : method.Resolve();
            var mdi = !method.Module.HasSymbols || methoddef is null
                ? null
                : TryRead(methoddef);
            SequencePoint sp = null;
            if (mdi is object) {
                if (instruction is object)
                    sp = mdi.GetSequencePoint(instruction);
                if (methoddef is null)
                    methoddef = method.Resolve();
                if (sp is null && useAnySequencePoint && methoddef is object) {
                    for (var i = 0; i < methoddef.Body.Instructions.Count; i++)
                    {
                        var op = methoddef.Body.Instructions[i];
                        var temp = mdi.GetSequencePoint(op);
                        if (temp is object) sp = temp;
                        // prefer the last sequence point before the provided instruction argument
                        if (sp is object && (ReferenceEquals(op, instruction) || instruction is null)) break;
                    }
                }
            }
            return sp;
        }
        public static void Patched(this TextWriter logger, bool error, string diagnostic, string processor, MethodReference method, Instruction instruction = null, bool useAnySequencePoint = true, [CallerMemberName] string caller = null, string additionalInfo = null) {
            var sequencePoint = FindSequencePoint(method, instruction, useAnySequencePoint);
            if (diagnostic is null) diagnostic = "STRIDE001";
            var message = $"'{processor}.{caller}' patched '{method.FullName}'";
            Diagnostic(logger, error, diagnostic, processor, message, sequencePoint, instruction, caller, additionalInfo);
        }
        public static void Diagnostic(this TextWriter logger, bool error, string diagnostic, string processor, string message, SequencePoint sequencePoint = null, Instruction instruction = null, [CallerMemberName] string caller = null, string additionalInfo = null) {
            var severity = error ? "error" : "warning";
            if (diagnostic is null) diagnostic = "STRIDE000";
            string location = null;                                                                 // foo.cs(sl,sp,el,ep):
            var pre = $"{severity} {diagnostic}:";                                                  // warning CS0000:
            if (string.IsNullOrEmpty(message)) message = $"'{processor}.{caller}' made a patch";    // Error text
            string nocode = null;                                                                   // at IL0000: ldc.i4 0xdeadbeef
                                                                                                    // .
            if (sequencePoint is object)
                location = L(sequencePoint);
            else if (instruction is object)
                nocode = $"at IL{instruction.Offset:x4}: {string.Join(" ", S(instruction.OpCode), S(instruction.Operand))}";

            var diagline = string.Join(" ", new[] { location, pre, message, nocode }.Where(s => !string.IsNullOrEmpty(s)).ToArray()) + '.';
            if (!string.IsNullOrEmpty(additionalInfo))
                diagline = string.Join(" ", diagline, additionalInfo);
            logger.WriteLine(diagline);

            string S(object o) => o?.ToString() ?? "";
            string L(SequencePoint sp) => $"{sp.Document.Url}({sp.StartLine},{sp.StartColumn},{sp.EndLine},{sp.EndColumn}):";
        }

    }
}
