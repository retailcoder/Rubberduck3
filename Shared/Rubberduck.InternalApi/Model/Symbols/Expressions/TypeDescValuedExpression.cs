using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Types.Abstract;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions;

public record class TypeDescValuedExpression : ValuedExpression<VBTypeDescValue>
{
    public TypeDescValuedExpression(WorkspaceUri parentUri, VBType type)
        : base(RubberduckSymbolKind.Class, VBTypeDesc.TypeInfo, type.Name, parentUri)
    {
        ReferredType = type;
    }
    public VBType ReferredType { get; }
    public override VBTypeDescValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        return new VBTypeDescValue(ReferredType);
    }
}
