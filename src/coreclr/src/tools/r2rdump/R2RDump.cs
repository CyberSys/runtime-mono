// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ILCompiler.Reflection.ReadyToRun;

namespace R2RDump
{
    public class DumpOptions : IAssemblyResolver
    {
        public FileInfo[] In { get; set; }
        public FileInfo Out { get; set; }
        public bool Raw { get; set; }
        public bool Header { get; set; }
        public bool Disasm { get; set; }
        public bool Naked { get; set; }

        public string[] Query { get; set; }
        public string[] Keyword { get; set; }
        public string[] RuntimeFunction { get; set; }
        public string[] Section { get; set; }

        public bool Unwind { get; set; }
        public bool GC { get; set; }
        public bool SectionContents { get; set; }
        public bool EntryPoints { get; set; }
        public bool Normalize { get; set; }
        public bool Verbose { get; set; }
        public bool Diff { get; set; }
        public bool IgnoreSensitive { get; set; }

        public FileInfo[] Reference { get; set; }
        public DirectoryInfo[] ReferencePath { get; set; }

        public bool SignatureBinary { get; set; }
        public bool InlineSignatureBinary { get; set; }

        /// <summary>
        /// Probing extensions to use when looking up assemblies under reference paths.
        /// </summary>
        private readonly static string[] ProbeExtensions = new string[] { ".ni.exe", ".ni.dll", ".exe", ".dll" };

        /// <summary>
        /// Try to locate a (reference) assembly using the list of explicit reference assemblies
        /// and the list of reference paths passed to R2RDump.
        /// </summary>
        /// <param name="simpleName">Simple name of the assembly to look up</param>
        /// <param name="parentFile">Name of assembly from which we're performing the lookup</param>
        /// <returns></returns>

        public MetadataReader FindAssembly(MetadataReader metadataReader, AssemblyReferenceHandle assemblyReferenceHandle, string parentFile)
        {
            string simpleName = metadataReader.GetString(metadataReader.GetAssemblyReference(assemblyReferenceHandle).Name);
            foreach (FileInfo refAsm in Reference ?? Enumerable.Empty<FileInfo>())
            {
                if (Path.GetFileNameWithoutExtension(refAsm.FullName).Equals(simpleName, StringComparison.OrdinalIgnoreCase))
                {
                    return Open(refAsm.FullName);
                }
            }

            IEnumerable<string> allRefPaths = new string[] { Path.GetDirectoryName(parentFile) }
                .Concat((ReferencePath ?? Enumerable.Empty<DirectoryInfo>()).Select(path => path.FullName));

            foreach (string refPath in allRefPaths)
            {
                foreach (string extension in ProbeExtensions)
                {
                    string probeFile = Path.Combine(refPath, simpleName + extension);
                    if (File.Exists(probeFile))
                    {
                        return Open(probeFile);
                    }
                }
            }

            return null;
        }

        private static unsafe MetadataReader Open(string filename)
        {
            byte[] image = File.ReadAllBytes(filename);

            PEReader peReader = new PEReader(Unsafe.As<byte[], ImmutableArray<byte>>(ref image));

            if (!peReader.HasMetadata)
            {
                throw new Exception($"ECMA metadata not found in file '{filename}'");
            }

            return peReader.GetMetadataReader();
        }
    }

    public abstract class Dumper
    {
        protected readonly ReadyToRunReader _r2r;
        protected TextWriter _writer;
        protected readonly Disassembler _disassembler;
        protected readonly DumpOptions _options;

        public Dumper(ReadyToRunReader r2r, TextWriter writer, Disassembler disassembler, DumpOptions options)
        {
            _r2r = r2r;
            _writer = writer;
            _disassembler = disassembler;
            _options = options;
        }

        public IEnumerable<ReadyToRunSection> NormalizedSections()
        {
            IEnumerable<ReadyToRunSection> sections = _r2r.ReadyToRunHeader.Sections.Values;
            if (_options.Normalize)
            {
                sections = sections.OrderBy((s) => s.Type);
            }
            return sections;
        }

        public IEnumerable<ReadyToRunMethod> NormalizedMethods()
        {
            IEnumerable<ReadyToRunMethod> methods = _r2r.R2RMethods;
            if (_options.Normalize)
            {
                methods = methods.OrderBy((m) => m.SignatureString);
            }
            return methods;
        }

        /// <summary>
        /// Run right before printing output
        /// </summary>
        abstract internal void Begin();

        /// <summary>
        /// Run right after printing output
        /// </summary>
        abstract internal void End();
        abstract internal void WriteDivider(string title);
        abstract internal void WriteSubDivider();
        abstract internal void SkipLine();
        abstract internal void DumpHeader(bool dumpSections);
        abstract internal void DumpSection(ReadyToRunSection section);
        abstract internal void DumpEntryPoints();
        abstract internal void DumpAllMethods();
        abstract internal void DumpMethod(ReadyToRunMethod method);
        abstract internal void DumpRuntimeFunction(RuntimeFunction rtf);
        abstract internal void DumpDisasm(RuntimeFunction rtf, int imageOffset);
        abstract internal void DumpBytes(int rva, uint size, string name = "Raw", bool convertToOffset = true);
        abstract internal void DumpSectionContents(ReadyToRunSection section);
        abstract internal void DumpQueryCount(string q, string title, int count);
    }

    class R2RDump
    {
        private readonly DumpOptions _options;
        private readonly TextWriter _writer;
        private readonly Dictionary<ReadyToRunSection.SectionType, bool> _selectedSections = new Dictionary<ReadyToRunSection.SectionType, bool>();
        private Dumper _dumper;

        private R2RDump(DumpOptions options)
        {
            _options = options;

            if (_options.Verbose)
            {
                _options.Disasm = true;
                _options.Unwind = true;
                _options.GC = true;
                _options.SectionContents = true;
            }

            // open output stream
            if (_options.Out != null)
            {
                _writer = new StreamWriter(_options.Out.FullName, append: false, encoding: Encoding.ASCII);
            }
            else
            {
                _writer = Console.Out;
            }
        }

        private static int ArgStringToInt(string arg)
        {
            int n;
            if (!ArgStringToInt(arg, out n))
            {
                throw new ArgumentException("Can't parse argument to int");
            }
            return n;
        }

        /// <summary>
        /// Converts string passed as cmd line args into int, works for hexadecimal with 0x as prefix
        /// </summary>
        /// <param name="arg">The argument string to convert</param>
        /// <param name="n">The integer representation</param>
        private static bool ArgStringToInt(string arg, out int n)
        {
            arg = arg.Trim();
            if (arg.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return int.TryParse(arg.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out n);
            }
            return int.TryParse(arg, out n);
        }

        /// <summary>
        /// Outputs a warning message
        /// </summary>
        /// <param name="warning">The warning message to output</param>
        public static void WriteWarning(string warning)
        {
            Console.WriteLine($"Warning: {warning}");
        }

        // <summary>
        /// For each query in the list of queries, dump all methods matching the query by name, signature or id
        /// </summary>
        /// <param name="r2r">Contains all the extracted info about the ReadyToRun image</param>
        /// <param name="title">The title to print, "R2R Methods by Query" or "R2R Methods by Keyword"</param>
        /// <param name="queries">The keywords/ids to search for</param>
        /// <param name="exact">Specifies whether to look for methods with names/signatures/ids matching the method exactly or partially</param>
        private void QueryMethod(ReadyToRunReader r2r, string title, IReadOnlyList<string> queries, bool exact)
        {
            if (queries.Count > 0)
            {
                _dumper.WriteDivider(title);
            }
            foreach (string q in queries)
            {
                IList<ReadyToRunMethod> res = FindMethod(r2r, q, exact);
                _dumper.DumpQueryCount(q, "Methods", res.Count);
                foreach (ReadyToRunMethod method in res)
                {
                    _dumper.DumpMethod(method);
                }
            }
        }

        // <summary>
        /// For each query in the list of queries, dump all sections by the name or value of the ReadyToRunSectionType enum
        /// </summary>
        /// <param name="r2r">Contains all the extracted info about the ReadyToRun image</param>
        /// <param name="queries">The names/values to search for</param>
        private void QuerySection(ReadyToRunReader r2r, IReadOnlyList<string> queries)
        {
            if (queries.Count > 0)
            {
                _dumper.WriteDivider("R2R Section");
            }
            foreach (string q in queries)
            {
                IList<ReadyToRunSection> res = FindSection(r2r, q);
                _dumper.DumpQueryCount(q, "Sections", res.Count);
                foreach (ReadyToRunSection section in res)
                {
                    _dumper.DumpSection(section);
                }
            }
        }

        // <summary>
        /// For each query in the list of queries, dump a runtime function by id.
        /// The method containing the runtime function gets outputted, along with the single runtime function that was searched
        /// </summary>
        /// <param name="r2r">Contains all the extracted info about the ReadyToRun image</param>
        /// <param name="queries">The ids to search for</param>
        private void QueryRuntimeFunction(ReadyToRunReader r2r, IEnumerable<string> queries)
        {
            if (queries.Any())
            {
                _dumper.WriteDivider("Runtime Functions");
            }
            foreach (string q in queries)
            {
                RuntimeFunction rtf = FindRuntimeFunction(r2r, ArgStringToInt(q));

                if (rtf == null)
                {
                    WriteWarning("Unable to find by id " + q);
                    continue;
                }
                _dumper.DumpQueryCount(q.ToString(), "Runtime Function", 1);
                _dumper.DumpRuntimeFunction(rtf);
            }
        }

        /// <summary>
        /// Outputs specified headers, sections, methods or runtime functions for one ReadyToRun image
        /// </summary>
        /// <param name="r2r">The structure containing the info of the ReadyToRun image</param>
        public void Dump(ReadyToRunReader r2r)
        {
            _dumper.Begin();

            if (_options.Header || !_options.EntryPoints)
            {
                _dumper.WriteDivider("R2R Header");
                _dumper.DumpHeader(true);
            }

            bool haveQuery = false;
            if (_options.Section != null)
            {
                haveQuery = true;
                QuerySection(r2r, _options.Section);
            }

            if (_options.RuntimeFunction != null)
            {
                haveQuery = true;
                QueryRuntimeFunction(r2r, _options.RuntimeFunction);
            }

            if (_options.Query != null)
            {
                haveQuery = true;
                QueryMethod(r2r, "R2R Methods by Query", _options.Query, true);
            }

            if (_options.Keyword != null)
            {
                haveQuery = true;
                QueryMethod(r2r, "R2R Methods by Keyword", _options.Keyword, false);
            }

            if (!haveQuery)
            {
                // Dump all sections and methods if no queries specified
                if (_options.EntryPoints)
                {
                    _dumper.DumpEntryPoints();
                }

                if (!_options.Header && !_options.EntryPoints)
                {
                    _dumper.DumpAllMethods();
                }
            }

            _dumper.End();
        }

        /// <summary>
        /// Returns true if the name, signature or id of <param>method</param> matches <param>query</param>
        /// </summary>
        /// <param name="exact">Specifies exact or partial match</param>
        /// <remarks>Case-insensitive and ignores whitespace</remarks>
        private bool Match(ReadyToRunMethod method, string query, bool exact)
        {
            int id;
            bool isNum = ArgStringToInt(query, out id);
            bool idMatch = isNum && (method.Rid == id || MetadataTokens.GetRowNumber(method.MetadataReader, method.MethodHandle) == id);

            bool sigMatch = false;
            if (exact)
            {
                sigMatch = method.Name.Equals(query, StringComparison.OrdinalIgnoreCase);
                if (!sigMatch)
                {
                    string sig = method.SignatureString.Replace(" ", "");
                    string q = query.Replace(" ", "");
                    int iMatch = sig.IndexOf(q, StringComparison.OrdinalIgnoreCase);
                    sigMatch = (iMatch == 0 || (iMatch > 0 && iMatch == (sig.Length - q.Length) && sig[iMatch - 1] == '.'));
                }
            }
            else
            {
                string sig = method.Signature.ReturnType + method.SignatureString.Replace(" ", "");
                sigMatch = (sig.IndexOf(query.Replace(" ", ""), StringComparison.OrdinalIgnoreCase) >= 0);
            }

            return idMatch || sigMatch;
        }

        /// <summary>
        /// Returns true if the name or value of the ReadyToRunSectionType of <param>section</param> matches <param>query</param>
        /// </summary>
        /// <remarks>Case-insensitive</remarks>
        private bool Match(ReadyToRunSection section, string query)
        {
            int queryInt;
            bool isNum = ArgStringToInt(query, out queryInt);
            string typeName = Enum.GetName(typeof(ReadyToRunSection.SectionType), section.Type);

            return (isNum && (int)section.Type == queryInt) || typeName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Finds all R2RMethods by name/signature/id matching <param>query</param>
        /// </summary>
        /// <param name="r2r">Contains all extracted info about the ReadyToRun image</param>
        /// <param name="query">The name/signature/id to search for</param>
        /// <param name="exact">Specifies exact or partial match</param>
        /// <out name="res">List of all matching methods</out>
        /// <remarks>Case-insensitive and ignores whitespace</remarks>
        public IList<ReadyToRunMethod> FindMethod(ReadyToRunReader r2r, string query, bool exact)
        {
            List<ReadyToRunMethod> res = new List<ReadyToRunMethod>();
            foreach (ReadyToRunMethod method in r2r.R2RMethods)
            {
                if (Match(method, query, exact))
                {
                    res.Add(method);
                }
            }
            return res;
        }

        /// <summary>
        /// Finds all R2RSections by name or value of the ReadyToRunSectionType matching <param>query</param>
        /// </summary>
        /// <param name="r2r">Contains all extracted info about the ReadyToRun image</param>
        /// <param name="query">The name or value to search for</param>
        /// <out name="res">List of all matching sections</out>
        /// <remarks>Case-insensitive</remarks>
        public IList<ReadyToRunSection> FindSection(ReadyToRunReader r2r, string query)
        {
            List<ReadyToRunSection> res = new List<ReadyToRunSection>();
            foreach (ReadyToRunSection section in r2r.ReadyToRunHeader.Sections.Values)
            {
                if (Match(section, query))
                {
                    res.Add(section);
                }
            }
            return res;
        }

        /// <summary>
        /// Returns the runtime function with id matching <param>rtfQuery</param>
        /// </summary>
        /// <param name="r2r">Contains all extracted info about the ReadyToRun image</param>
        /// <param name="rtfQuery">The name or value to search for</param>
        public RuntimeFunction FindRuntimeFunction(ReadyToRunReader r2r, int rtfQuery)
        {
            foreach (ReadyToRunMethod m in r2r.R2RMethods)
            {
                foreach (RuntimeFunction rtf in m.RuntimeFunctions)
                {
                    if (rtf.Id == rtfQuery || (rtf.StartAddress >= rtfQuery && rtf.StartAddress + rtf.Size < rtfQuery))
                    {
                        return rtf;
                    }
                }
            }
            return null;
        }

        private int Run()
        {
            Disassembler disassembler = null;

            try
            {
                if (_options.In == null || _options.In.Length == 0)
                    throw new ArgumentException("Input filename must be specified (--in <file>)");

                if (_options.Diff && _options.In.Length < 2)
                    throw new ArgumentException("Need at least 2 input files in diff mode");

                if (_options.Naked && _options.Raw)
                {
                    throw new ArgumentException("The option '--naked' is incompatible with '--raw'");
                }

                ReadyToRunReader previousReader = null;

                foreach (FileInfo filename in _options.In)
                {
                    // parse the ReadyToRun image
                    ReadyToRunReader r2r = new ReadyToRunReader(_options, filename.FullName);

                    if (_options.Disasm)
                    {
                        if (r2r.InputArchitectureSupported() && r2r.DisassemblerArchitectureSupported())
                        {
                            disassembler = new Disassembler(r2r, _options);
                        }
                        else
                        {
                            throw new ArgumentException($"The architecture of input file {filename} ({r2r.Machine.ToString()}) or the architecture of the disassembler tools ({System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString()}) is not supported.");
                        }
                    }

                    _dumper = new TextDumper(r2r, _writer, disassembler, _options);

                    if (!_options.Diff)
                    {
                        // output the ReadyToRun info
                        Dump(r2r);
                    }
                    else if (previousReader != null)
                    {
                        new R2RDiff(previousReader, r2r, _writer).Run();
                    }

                    previousReader = r2r;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.ToString());
                if (e is ArgumentException)
                {
                    Console.WriteLine();
                }
                return 1;
            }
            finally
            {
                if (disassembler != null)
                {
                    disassembler.Dispose();
                }
                // close output stream
                _writer.Close();
            }

            return 0;
        }

        public static async Task<int> Main(string[] args)
        {
            var command = CommandLineOptions.RootCommand();
            command.Handler = CommandHandler.Create<DumpOptions>((DumpOptions options) => new R2RDump(options).Run());
            return await command.InvokeAsync(args);
        }
    }
}
