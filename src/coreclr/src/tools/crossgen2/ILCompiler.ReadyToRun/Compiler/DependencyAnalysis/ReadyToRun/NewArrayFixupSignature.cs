// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Internal.ReadyToRunConstants;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class NewArrayFixupSignature : Signature
    {
        private readonly ArrayType _arrayType;
        private readonly SignatureContext _signatureContext;

        public NewArrayFixupSignature(ArrayType arrayType, SignatureContext signatureContext)
        {
            _arrayType = arrayType;
            _signatureContext = signatureContext;

            // Ensure types in signature are loadable and resolvable, otherwise we'll fail later while emitting the signature
            signatureContext.Resolver.CompilerContext.EnsureLoadableType(arrayType);
        }

        public override int ClassCode => 815543321;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ReadyToRunCodegenNodeFactory r2rFactory = (ReadyToRunCodegenNodeFactory)factory;
            ObjectDataSignatureBuilder dataBuilder = new ObjectDataSignatureBuilder();

            if (!relocsOnly)
            {
                dataBuilder.AddSymbol(this);

                EcmaModule targetModule = _signatureContext.GetTargetModule(_arrayType);
                SignatureContext innerContext = dataBuilder.EmitFixup(r2rFactory, ReadyToRunFixupKind.NewArray, targetModule, _signatureContext);
                dataBuilder.EmitTypeSignature(_arrayType, innerContext);
            }

            return dataBuilder.ToObjectData();
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append($@"NewArraySignature: ");
            sb.Append(nameMangler.GetMangledTypeName(_arrayType));
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            NewArrayFixupSignature otherNode = (NewArrayFixupSignature)other;
            int result = comparer.Compare(_arrayType, otherNode._arrayType);
            if (result != 0)
                return result;

            return _signatureContext.CompareTo(otherNode._signatureContext, comparer);
        }

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            DependencyList dependencies = new DependencyList();
            dependencies.Add(factory.NecessaryTypeSymbol(_arrayType.ElementType), "Type used as array element");
            return dependencies;
        }
    }
}
