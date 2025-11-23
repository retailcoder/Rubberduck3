using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Types.Abstract;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using System.Collections.Generic;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions.Operators;

public abstract record class OperatorExpression<TValue> : ValuedExpression<TValue>
    where TValue : VBTypedValue
{
    protected OperatorExpression(string name, VBType vbType, WorkspaceUri parentUri, IEnumerable<ValuedExpression> children)
        : base(RubberduckSymbolKind.Operator, vbType, name, parentUri, children)
    {
    }
}
