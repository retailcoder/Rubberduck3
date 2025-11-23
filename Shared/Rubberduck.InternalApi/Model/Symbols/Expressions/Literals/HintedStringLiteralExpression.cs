using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions.Literals;

/// <summary>
/// Represents an explicitly-typed <c>String</c> (<c>$</c>) literal value.
/// </summary>
public record class HintedStringLiteralExpression : StringLiteralExpression
{
    public HintedStringLiteralExpression(WorkspaceUri parentUri, VBStringValue value)
        : base(parentUri, value)
    {
    }
}
