using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions.Literals;

public record class HexLiteralExpression : NumericLiteralExpression<VBLongValue>
{
    public HexLiteralExpression(WorkspaceUri parentUri, VBLongValue value)
        : base(parentUri, value)
    {
    }
}
