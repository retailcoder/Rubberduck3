using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols.Expressions;

namespace Rubberduck.InternalApi.Model.Symbols.Statements.FileIO;

public record class PrintStatement : FileStatement
{
    public PrintStatement(WorkspaceUri parentUri, ValuedExpression fileNumber, StringValuedExpression expression, LineLabelSymbol? parentLabel = null)
        : base(parentUri, fileNumber, parentLabel)
    {
        Expression = expression;
    }

    public StringValuedExpression Expression { get; init; }
}
