// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using ILCompiler.Reflection.ReadyToRun;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace R2RDump
{
    internal static class Extensions
    {
        public static void WriteTo(this DebugInfo theThis, TextWriter writer, DumpOptions dumpOptions)
        {
            if (theThis.BoundsList.Count > 0)
                writer.WriteLine("Debug Info");

            writer.WriteLine("\tBounds:");
            for (int i = 0; i < theThis.BoundsList.Count; ++i)
            {
                writer.Write('\t');
                if (!dumpOptions.Naked)
                {
                    writer.Write($"Native Offset: 0x{theThis.BoundsList[i].NativeOffset:X}, ");
                }
                writer.WriteLine($"IL Offset: 0x{theThis.BoundsList[i].ILOffset:X}, Source Types: {theThis.BoundsList[i].SourceTypes}");
            }

            writer.WriteLine("");

            if (dumpOptions.Normalize)
            {
                theThis.VariablesList.Sort(new NativeVarInfoComparer());
            }

            if (theThis.VariablesList.Count > 0)
                writer.WriteLine("\tVariable Locations:");

            for (int i = 0; i < theThis.VariablesList.Count; ++i)
            {
                var varLoc = theThis.VariablesList[i];
                writer.WriteLine($"\tVariable Number: {varLoc.VariableNumber}");
                writer.WriteLine($"\tStart Offset: 0x{varLoc.StartOffset:X}");
                writer.WriteLine($"\tEnd Offset: 0x{varLoc.EndOffset:X}");
                writer.WriteLine($"\tLoc Type: {varLoc.VariableLocation.VarLocType}");

                switch (varLoc.VariableLocation.VarLocType)
                {
                    case VarLocType.VLT_REG:
                    case VarLocType.VLT_REG_FP:
                    case VarLocType.VLT_REG_BYREF:
                        writer.WriteLine($"\tRegister: {DebugInfo.GetPlatformSpecificRegister(theThis.Machine, varLoc.VariableLocation.Data1)}");
                        break;
                    case VarLocType.VLT_STK:
                    case VarLocType.VLT_STK_BYREF:
                        writer.WriteLine($"\tBase Register: {DebugInfo.GetPlatformSpecificRegister(theThis.Machine, varLoc.VariableLocation.Data1)}");
                        writer.WriteLine($"\tStack Offset: {varLoc.VariableLocation.Data2}");
                        break;
                    case VarLocType.VLT_REG_REG:
                        writer.WriteLine($"\tRegister 1: {DebugInfo.GetPlatformSpecificRegister(theThis.Machine, varLoc.VariableLocation.Data1)}");
                        writer.WriteLine($"\tRegister 2: {DebugInfo.GetPlatformSpecificRegister(theThis.Machine, varLoc.VariableLocation.Data2)}");
                        break;
                    case VarLocType.VLT_REG_STK:
                        writer.WriteLine($"\tRegister: {DebugInfo.GetPlatformSpecificRegister(theThis.Machine, varLoc.VariableLocation.Data1)}");
                        writer.WriteLine($"\tBase Register: {DebugInfo.GetPlatformSpecificRegister(theThis.Machine, varLoc.VariableLocation.Data2)}");
                        writer.WriteLine($"\tStack Offset: {varLoc.VariableLocation.Data3}");
                        break;
                    case VarLocType.VLT_STK_REG:
                        writer.WriteLine($"\tStack Offset: {varLoc.VariableLocation.Data1}");
                        writer.WriteLine($"\tBase Register: {DebugInfo.GetPlatformSpecificRegister(theThis.Machine, varLoc.VariableLocation.Data2)}");
                        writer.WriteLine($"\tRegister: {DebugInfo.GetPlatformSpecificRegister(theThis.Machine, varLoc.VariableLocation.Data3)}");
                        break;
                    case VarLocType.VLT_STK2:
                        writer.WriteLine($"\tBase Register: {DebugInfo.GetPlatformSpecificRegister(theThis.Machine, varLoc.VariableLocation.Data1)}");
                        writer.WriteLine($"\tStack Offset: {varLoc.VariableLocation.Data2}");
                        break;
                    case VarLocType.VLT_FPSTK:
                        writer.WriteLine($"\tOffset: {DebugInfo.GetPlatformSpecificRegister(theThis.Machine, varLoc.VariableLocation.Data1)}");
                        break;
                    case VarLocType.VLT_FIXED_VA:
                        writer.WriteLine($"\tOffset: {DebugInfo.GetPlatformSpecificRegister(theThis.Machine, varLoc.VariableLocation.Data1)}");
                        break;
                    default:
                        throw new BadImageFormatException("Unexpected var loc type");
                }

                writer.WriteLine("");
            }
        }

        public static void WriteTo(this ReadyToRunImportSection.ImportSectionEntry theThis, TextWriter writer, DumpOptions options)
        {
            if (!options.Naked)
            {
                writer.Write($"+{theThis.StartOffset:X4}");
                writer.Write($" ({theThis.StartRVA:X4})");
                writer.Write($"  Section: 0x{theThis.Section:X8}");
                writer.Write($"  SignatureRVA: 0x{theThis.SignatureRVA:X8}");
                writer.Write("   ");
            }
            writer.Write(theThis.Signature);
            if (theThis.GCRefMap != null)
            {
                writer.Write(" -- ");
                theThis.GCRefMap.WriteTo(writer);
            }
        }

        public static void WriteTo(this ReadyToRunSection theThis, TextWriter writer, DumpOptions options)
        {
            writer.WriteLine($"Type:  {Enum.GetName(typeof(ReadyToRunSection.SectionType), theThis.Type)} ({theThis.Type:D})");
            if (!options.Naked)
            {
                writer.WriteLine($"RelativeVirtualAddress: 0x{theThis.RelativeVirtualAddress:X8}");
            }
            writer.WriteLine($"Size: {theThis.Size} bytes");
        }

        public static void WriteTo(this ReadyToRunMethod theThis, TextWriter writer, DumpOptions options)
        {
            writer.WriteLine(theThis.SignatureString);

            writer.WriteLine($"Handle: 0x{MetadataTokens.GetToken(theThis.MetadataReader, theThis.MethodHandle):X8}");
            writer.WriteLine($"Rid: {MetadataTokens.GetRowNumber(theThis.MetadataReader, theThis.MethodHandle)}");
            if (!options.Naked)
            {
                writer.WriteLine($"EntryPointRuntimeFunctionId: {theThis.EntryPointRuntimeFunctionId}");
            }
            writer.WriteLine($"Number of RuntimeFunctions: {theThis.RuntimeFunctions.Count}");
            if (theThis.Fixups != null)
            {
                writer.WriteLine($"Number of fixups: {theThis.Fixups.Count()}");
                IEnumerable<FixupCell> fixups = theThis.Fixups;
                if (options.Normalize)
                {
                    fixups = fixups.OrderBy((fc) => fc.Signature);
                }

                foreach (FixupCell cell in fixups)
                {
                    writer.Write("    ");
                    if (!options.Naked)
                    {
                        writer.Write($"TableIndex {cell.TableIndex}, Offset {cell.CellOffset:X4}: ");
                    }
                    writer.WriteLine(cell.Signature);
                }
            }
        }

        public static void WriteTo(this RuntimeFunction theThis, TextWriter writer, DumpOptions options)
        {
            if (!options.Naked)
            {
                writer.WriteLine($"Id: {theThis.Id}");
                writer.WriteLine($"StartAddress: 0x{theThis.StartAddress:X8}");
            }
            if (theThis.Size == -1)
            {
                writer.WriteLine("Size: Unavailable");
            }
            else
            {
                writer.WriteLine($"Size: {theThis.Size} bytes");
            }
            if (!options.Naked)
            {
                writer.WriteLine($"UnwindRVA: 0x{theThis.UnwindRVA:X8}");
            }
            if (theThis.UnwindInfo is ILCompiler.Reflection.ReadyToRun.Amd64.UnwindInfo amd64UnwindInfo)
            {
                string parsedFlags = "";
                if ((amd64UnwindInfo.Flags & (int)ILCompiler.Reflection.ReadyToRun.Amd64.UnwindFlags.UNW_FLAG_EHANDLER) != 0)
                {
                    parsedFlags += " EHANDLER";
                }
                if ((amd64UnwindInfo.Flags & (int)ILCompiler.Reflection.ReadyToRun.Amd64.UnwindFlags.UNW_FLAG_UHANDLER) != 0)
                {
                    parsedFlags += " UHANDLER";
                }
                if ((amd64UnwindInfo.Flags & (int)ILCompiler.Reflection.ReadyToRun.Amd64.UnwindFlags.UNW_FLAG_CHAININFO) != 0)
                {
                    parsedFlags += " CHAININFO";
                }
                if (parsedFlags.Length == 0)
                {
                    parsedFlags = " NHANDLER";
                }
                writer.WriteLine($"Version:            {amd64UnwindInfo.Version}");
                writer.WriteLine($"Flags:              0x{amd64UnwindInfo.Flags:X2}{parsedFlags}");
                writer.WriteLine($"SizeOfProlog:       0x{amd64UnwindInfo.SizeOfProlog:X4}");
                writer.WriteLine($"CountOfUnwindCodes: {amd64UnwindInfo.CountOfUnwindCodes}");
                writer.WriteLine($"FrameRegister:      {amd64UnwindInfo.FrameRegister}");
                writer.WriteLine($"FrameOffset:        0x{amd64UnwindInfo.FrameOffset}");
                if (!options.Naked)
                {
                    writer.WriteLine($"PersonalityRVA:     0x{amd64UnwindInfo.PersonalityRoutineRVA:X4}");
                }

                for (int unwindCodeIndex = 0; unwindCodeIndex < amd64UnwindInfo.CountOfUnwindCodes; unwindCodeIndex++)
                {
                    ILCompiler.Reflection.ReadyToRun.Amd64.UnwindCode unwindCode = amd64UnwindInfo.UnwindCodeArray[unwindCodeIndex];
                    writer.Write($"UnwindCode[{unwindCode.Index}]: ");
                    writer.Write($"CodeOffset 0x{unwindCode.CodeOffset:X4} ");
                    writer.Write($"FrameOffset 0x{unwindCode.FrameOffset:X4} ");
                    writer.Write($"NextOffset 0x{unwindCode.NextFrameOffset} ");
                    writer.Write($"Op {unwindCode.OpInfoStr}");
                    writer.WriteLine();
                }
            }
            writer.WriteLine();

            if (theThis.Method.GcInfo is ILCompiler.Reflection.ReadyToRun.Amd64.GcInfo gcInfo)
            {
                writer.WriteLine("GC info:");
                writer.WriteLine(gcInfo.ToString());
            }

            if (theThis.EHInfo != null)
            {
                writer.WriteLine($@"EH info @ {theThis.EHInfo.EHInfoRVA:X4}, #clauses = {theThis.EHInfo.EHClauses.Length}");
                theThis.EHInfo.WriteTo(writer);
                writer.WriteLine();
            }

            if (theThis.DebugInfo != null)
            {
                theThis.DebugInfo.WriteTo(writer, options);
            }
        }

        public static void WriteTo(this GCRefMap theThis, TextWriter writer)
        {
            if (theThis.StackPop != GCRefMap.InvalidStackPop)
            {
                writer.Write(@"POP(0x{StackPop:X}) ");
            }
            for (int entryIndex = 0; entryIndex < theThis.Entries.Length; entryIndex++)
            {
                GCRefMapEntry entry = theThis.Entries[entryIndex];
                if (entryIndex == 0 || entry.Token != theThis.Entries[entryIndex - 1].Token)
                {
                    if (entryIndex != 0)
                    {
                        writer.Write(") ");
                    }
                    switch (entry.Token)
                    {
                        case CORCOMPILE_GCREFMAP_TOKENS.GCREFMAP_REF:
                            writer.Write("R");
                            break;
                        case CORCOMPILE_GCREFMAP_TOKENS.GCREFMAP_INTERIOR:
                            writer.Write("I");
                            break;
                        case CORCOMPILE_GCREFMAP_TOKENS.GCREFMAP_METHOD_PARAM:
                            writer.Write("M");
                            break;
                        case CORCOMPILE_GCREFMAP_TOKENS.GCREFMAP_TYPE_PARAM:
                            writer.Write("T");
                            break;
                        case CORCOMPILE_GCREFMAP_TOKENS.GCREFMAP_VASIG_COOKIE:
                            writer.Write("V");
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                    writer.Write("(");
                }
                else
                {
                    writer.Write(" ");
                }
                writer.Write($"{entry.Offset:X2}");
            }
            writer.Write(")");
        }
    }
}
