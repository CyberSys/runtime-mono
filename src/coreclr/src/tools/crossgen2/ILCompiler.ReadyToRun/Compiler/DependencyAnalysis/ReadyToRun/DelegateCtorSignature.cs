// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.TypeSystem;
using Internal.JitInterface;
using Internal.Text;
using Internal.ReadyToRunConstants;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class DelegateCtorSignature : Signature
    {
        private readonly TypeDesc _delegateType;

        private readonly IMethodNode _targetMethod;

        private readonly ModuleToken _methodToken;

        private readonly SignatureContext _signatureContext;

        public DelegateCtorSignature(
            TypeDesc delegateType,
            IMethodNode targetMethod,
            ModuleToken methodToken,
            SignatureContext signatureContext)
        {
            _delegateType = delegateType;
            _targetMethod = targetMethod;
            _methodToken = methodToken;
            _signatureContext = signatureContext;

            // Ensure types in signature are loadable and resolvable, otherwise we'll fail later while emitting the signature
            signatureContext.Resolver.CompilerContext.EnsureLoadableType(delegateType);
            signatureContext.Resolver.CompilerContext.EnsureLoadableMethod(targetMethod.Method);
        }

        public override int ClassCode => 99885741;

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ReadyToRunCodegenNodeFactory r2rFactory = (ReadyToRunCodegenNodeFactory)factory;
            ObjectDataSignatureBuilder builder = new ObjectDataSignatureBuilder();
            builder.AddSymbol(this);

            if (!relocsOnly)
            {
                SignatureContext innerContext = builder.EmitFixup(r2rFactory, ReadyToRunFixupKind.DelegateCtor, _methodToken.Module, _signatureContext);

                builder.EmitMethodSignature(
                    new MethodWithToken(_targetMethod.Method, _methodToken, constrainedType: null),
                    enforceDefEncoding: false,
                    enforceOwningType: false,
                    innerContext,
                    isUnboxingStub: false,
                    isInstantiatingStub: _targetMethod.Method.HasInstantiation);

                builder.EmitTypeSignature(_delegateType, innerContext);
            }

            return builder.ToObjectData();
        }

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            return new DependencyList(
                new DependencyListEntry[]
                {
                    new DependencyListEntry(_targetMethod, "Delegate target method")
                }
            );
        }

        public override void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb)
        {
            sb.Append(nameMangler.CompilationUnitPrefix);
            sb.Append($@"DelegateCtor(");
            sb.Append(nameMangler.GetMangledTypeName(_delegateType));
            sb.Append(" -> ");
            sb.Append(nameMangler.GetMangledMethodName(_targetMethod.Method));
            sb.Append("; ");
            sb.Append(_methodToken.ToString());
            sb.Append(")");
        }

        public override int CompareToImpl(ISortableNode other, CompilerComparer comparer)
        {
            DelegateCtorSignature otherNode = (DelegateCtorSignature)other;
            int result = comparer.Compare(_delegateType, otherNode._delegateType);
            if (result != 0)
                return result;

            result = comparer.Compare(_targetMethod.Method, otherNode._targetMethod.Method);
            if (result != 0)
                return result;

            result = _methodToken.CompareTo(otherNode._methodToken);
            if (result != 0)
                return result;

            return _signatureContext.CompareTo(otherNode._signatureContext, comparer);
        }
    }
}
