// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

using Internal.JitInterface;
using Internal.Text;
using Internal.TypeSystem;
using Internal.ReadyToRunConstants;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    /// <summary>
    /// This class represents a single indirection cell used to call delay load helpers.
    /// In addition to PrecodeHelperImport instances of this import type emit GC ref map
    /// entries into the R2R executable.
    /// </summary>
    public class DelayLoadHelperMethodImport : DelayLoadHelperImport, IMethodNode
    {
        private readonly MethodWithToken _method;

        private readonly bool _useInstantiatingStub;

        private readonly SignatureContext _signatureContext;

        public DelayLoadHelperMethodImport(
            ReadyToRunCodegenNodeFactory factory, 
            ImportSectionNode importSectionNode, 
            ReadyToRunHelper helper, 
            MethodWithToken method,
            bool useVirtualCall,
            bool useInstantiatingStub,
            Signature instanceSignature, 
            SignatureContext signatureContext,
            string callSite = null)
            : base(factory, importSectionNode, helper, instanceSignature, useVirtualCall, callSite)
        {
            _method = method;
            _useInstantiatingStub = useInstantiatingStub;
            _signatureContext = signatureContext;
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            foreach (DependencyListEntry baseEntry in base.GetStaticDependencies(factory))
            {
                yield return baseEntry;
            }
            if (_useInstantiatingStub)
            {
                // Require compilation of the canonical version for instantiating stubs
                MethodDesc canonMethod = _method.Method.GetCanonMethodTarget(CanonicalFormKind.Specific);
                ReadyToRunCodegenNodeFactory r2rFactory = (ReadyToRunCodegenNodeFactory)factory;
                ISymbolNode canonMethodNode = r2rFactory.MethodEntrypoint(
                    new MethodWithToken(canonMethod, _method.Token, constrainedType: null),
                    isUnboxingStub: false,
                    isInstantiatingStub: false,
                    isPrecodeImportRequired: false,
                    signatureContext: _signatureContext);
                yield return new DependencyListEntry(canonMethodNode, "Canonical method for instantiating stub");
            }
        }

        public override int ClassCode => 192837465;

        public MethodDesc Method => _method.Method;

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            DelayLoadHelperMethodImport otherNode = (DelayLoadHelperMethodImport)other;
            int result = _useInstantiatingStub.CompareTo(otherNode._useInstantiatingStub);
            if (result != 0)
                return result;

            result = _signatureContext.CompareTo(otherNode._signatureContext, comparer);
            if (result != 0)
                return result;

            result = _method.CompareTo(otherNode._method, comparer);
            if (result != 0)
                return result;

            return base.CompareToImpl(other, comparer);
        }
    }
}
