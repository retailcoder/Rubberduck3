using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions.Literals;

public record class EmptyLiteralExpression : VariantLiteralExpression
{
    public EmptyLiteralExpression(WorkspaceUri parentUri)
        : base(parentUri, VBEmptyValue.Empty.AsVariant())
    {
    }
}
