using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols.Expressions;

namespace Rubberduck.InternalApi.Model.Symbols.Statements.FileIO;

public record class SeekStatement : FileStatement
{
    public SeekStatement(WorkspaceUri parentUri,
        ValuedExpression fileNumber,
        ValuedExpression position,
        LineLabelSymbol? parentLabel = default)
        : base(parentUri, fileNumber, parentLabel)
    {
        Position = position;
    }

    public ValuedExpression Position { get; init; }
}
