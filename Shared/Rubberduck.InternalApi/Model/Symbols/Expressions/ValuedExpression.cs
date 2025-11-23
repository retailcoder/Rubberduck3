using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Types.Abstract;
using Rubberduck.InternalApi.Model.Symbols.Abstract;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using System.Collections.Generic;
using System.Linq;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions;

public abstract record class ValuedExpression : TypedSymbol, IExecutable<VBTypedValue>
{
    protected ValuedExpression(RubberduckSymbolKind kind, VBType vbType, string name, WorkspaceUri parentUri, IEnumerable<ValuedExpression>? children = null)
        : base(kind, Accessibility.Undefined, name, parentUri, children, vbType)
    {
    }

    public virtual VBTypedValue? Execute(VBExecutionContext context, bool rethrow = false) => default;
}

public abstract record class ValuedExpression<TValue> : ValuedExpression, IExecutable<TValue>
    where TValue : VBTypedValue
{
    protected ValuedExpression(RubberduckSymbolKind kind, VBType vbType, string name, WorkspaceUri parentUri, IEnumerable<ValuedExpression>? children = null)
        : base(kind, vbType, name, parentUri, children)
    {
        Type = vbType;
    }

    public VBType Type { get; init; }

    public override TValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        return Children?.OfType<ValuedExpression<TValue>>().FirstOrDefault()?.Execute(context, true);
    }
}

