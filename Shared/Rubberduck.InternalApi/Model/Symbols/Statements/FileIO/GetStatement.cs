using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols.Abstract;
using Rubberduck.InternalApi.Model.Symbols.Expressions;

namespace Rubberduck.InternalApi.Model.Symbols.Statements.FileIO;

public record class GetStatement : FileStatement
{
    public GetStatement(WorkspaceUri parentUri, ValuedExpression fileNumber,
        TypedSymbol variable,
        ValuedExpression? recordNumber = default,
        LineLabelSymbol? parentLabel = default)
        : base(parentUri, fileNumber, parentLabel)
    {
        Variable = variable;
        RecordNumber = recordNumber;
    }

    public TypedSymbol Variable { get; init; }
    public ValuedExpression? RecordNumber { get; init; }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        RecordNumber?.Execute(context, rethrow);
        context.SetSymbolValue(Variable, VBEmptyValue.Empty.AsVariant());
        return default;
    }
}
