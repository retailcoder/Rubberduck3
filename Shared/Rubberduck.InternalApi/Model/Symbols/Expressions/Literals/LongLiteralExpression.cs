using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions.Literals;

/// <summary>
/// Represents an explicitly-typed <c>Long</c> (<c>&</c>) literal value.
/// </summary>
public record class LongLiteralExpression : NumericLiteralExpression
{
    public LongLiteralExpression(WorkspaceUri parentUri, VBLongValue value)
        : base(parentUri, value)
    {
    }
}
