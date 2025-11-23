using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions.Literals;

/// <summary>
/// Represents an explicitly-typed <c>LongLong</c> (<c>^</c>) literal value.
/// </summary>
public record class LongLongLiteralExpression : NumericLiteralExpression
{
    public LongLongLiteralExpression(WorkspaceUri parentUri, VBLongLongValue value)
        : base(parentUri, value)
    {
    }
}
