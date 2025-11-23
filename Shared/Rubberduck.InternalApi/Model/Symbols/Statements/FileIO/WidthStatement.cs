using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols.Expressions;

namespace Rubberduck.InternalApi.Model.Symbols.Statements.FileIO;

public record class WidthStatement : FileStatement
{
    public WidthStatement(WorkspaceUri parentUri, ValuedExpression fileNumber, ValuedExpression lineWidth, LineLabelSymbol? parentLabel = null)
        : base(parentUri, fileNumber, parentLabel)
    {
        LineWidth = lineWidth;
    }

    public ValuedExpression LineWidth { get; init; }
}
