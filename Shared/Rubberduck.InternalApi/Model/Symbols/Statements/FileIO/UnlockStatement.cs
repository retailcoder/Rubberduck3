using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols.Expressions;

namespace Rubberduck.InternalApi.Model.Symbols.Statements.FileIO;

public record class UnlockStatement : FileStatement
{
    public UnlockStatement(WorkspaceUri parentUri,
        ValuedExpression fileNumber,
        ValuedExpression? startRecord = default,
        ValuedExpression? endRecord = default,
        LineLabelSymbol? parentLabel = default)
        : base(parentUri, fileNumber, parentLabel)
    {
        StartRecordNumber = startRecord;
        EndRecordNumber = endRecord;
    }

    public ValuedExpression? StartRecordNumber { get; init; }
    public ValuedExpression? EndRecordNumber { get; init; }
}
