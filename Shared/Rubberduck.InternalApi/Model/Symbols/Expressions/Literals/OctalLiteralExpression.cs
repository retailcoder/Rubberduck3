using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions.Literals;

public record class OctalLiteralExpression : NumericLiteralExpression<VBLongValue>
{
    public OctalLiteralExpression(WorkspaceUri parentUri, VBLongValue value)
        : base(parentUri, value)
    {
    }
}
