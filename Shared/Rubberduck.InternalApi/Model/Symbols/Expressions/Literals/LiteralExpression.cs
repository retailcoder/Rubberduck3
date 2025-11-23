using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols.Expressions;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions.Literals;

/// <summary>
/// Represents a literal expression.
/// </summary>
public record class LiteralExpression<TValue> : ValuedExpression<TValue> where TValue : VBTypedValue
{
    public LiteralExpression(RubberduckSymbolKind kind, WorkspaceUri parentUri, TValue value)
        : base(kind, value.TypeInfo, value.ToString(), parentUri)
    {
        Value = value;
    }

    public TValue Value { get; }

    public override TValue? Execute(VBExecutionContext context, bool rethrow = false) => Value;
}
