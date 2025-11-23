using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions.Literals;

/// <summary>
/// Represents an explicitly-typed <c>Double</c> (<c>#</c>) literal value.
/// </summary>
public record class DoubleLiteralExpression : NumericLiteralExpression
{
    public DoubleLiteralExpression(WorkspaceUri parentUri, VBDoubleValue value)
        : base(parentUri, value)
    {
    }
}
