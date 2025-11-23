using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions.Literals;

/// <summary>
/// Represents an explicitly-typed <c>Integer</c> (<c>%</c>) literal value.
/// </summary>
public record class IntegerLiteralExpression : NumericLiteralExpression
{
    public IntegerLiteralExpression(WorkspaceUri parentUri, VBIntegerValue value)
        : base(parentUri, value)
    {
    }
}
