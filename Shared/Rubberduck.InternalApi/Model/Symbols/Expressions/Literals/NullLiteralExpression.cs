using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions.Literals;

public record class NullLiteralExpression : VariantLiteralExpression
{
    public NullLiteralExpression(WorkspaceUri parentUri)
        : base(parentUri, VBNullValue.Null.AsVariant())
    {
    }
}
