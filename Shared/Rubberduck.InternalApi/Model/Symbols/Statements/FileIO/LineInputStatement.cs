using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols.Abstract;
using Rubberduck.InternalApi.Model.Symbols.Expressions;

namespace Rubberduck.InternalApi.Model.Symbols.Statements.FileIO;

public record class LineInputStatement : FileStatement
{
    public LineInputStatement(WorkspaceUri parentUri,
        ValuedExpression fileNumber,
        TypedSymbol variable,
        LineLabelSymbol? parentLabel = default)
        : base(parentUri, fileNumber, parentLabel)
    {
        Variable = variable;
    }

    public TypedSymbol Variable { get; init; }
}
