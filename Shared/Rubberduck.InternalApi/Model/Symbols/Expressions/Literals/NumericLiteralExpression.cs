using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions.Literals;

/// <summary>
/// Represents an implicitly-typed numeric literal value.
/// </summary>
public record class NumericLiteralExpression : LiteralExpression<VBNumericTypedValue>
{
    public NumericLiteralExpression(WorkspaceUri parentUri, VBNumericTypedValue value)
        : base(RubberduckSymbolKind.NumberLiteral, parentUri, value)
    {
    }
}

public record class NumericLiteralExpression<TValue> : LiteralExpression<TValue>
    where TValue : VBNumericTypedValue
{
    public NumericLiteralExpression(WorkspaceUri parentUri, TValue value)
        : base(RubberduckSymbolKind.NumberLiteral, parentUri, value)
    {
    }
}
