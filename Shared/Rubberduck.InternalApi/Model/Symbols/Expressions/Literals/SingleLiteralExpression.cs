using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions.Literals;

/// <summary>
/// Represents an explicitly-typed <c>Single</c> (<c>!</c>) literal value.
/// </summary>
public record class SingleLiteralExpression : NumericLiteralExpression
{
    public SingleLiteralExpression(WorkspaceUri parentUri, VBSingleValue value)
        : base(parentUri, value)
    {
    }
}
