using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols.Expressions;

namespace Rubberduck.InternalApi.Model.Symbols.Statements.FileIO;

public record class PutStatement : FileStatement
{
    public PutStatement(WorkspaceUri parentUri, ValuedExpression fileNumber,
        ValuedExpression dataExpression,
        ValuedExpression? recordNumber = default,
        LineLabelSymbol? parentLabel = default)
        : base(parentUri, fileNumber, parentLabel)
    {
        DataExpression = dataExpression;
        RecordNumber = recordNumber;
    }

    public ValuedExpression DataExpression { get; init; }
    public ValuedExpression? RecordNumber { get; init; }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        DataExpression.Execute(context, rethrow);
        RecordNumber?.Execute(context, rethrow);
        return default;
    }
}
