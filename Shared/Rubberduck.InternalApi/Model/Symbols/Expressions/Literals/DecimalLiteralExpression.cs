using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions.Literals;

/// <summary>
/// Represents an explicitly-typed <c>Currency</c> (<c>@</c>) literal value.
/// </summary>
public record class DecimalLiteralExpression : NumericLiteralExpression
{
    public DecimalLiteralExpression(WorkspaceUri parentUri, VBDecimalValue value)
        : base(parentUri, value)
    {
    }
}
