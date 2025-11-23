using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols.Abstract;
using Rubberduck.InternalApi.Model.Symbols.Expressions;
using System.Collections.Generic;

namespace Rubberduck.InternalApi.Model.Symbols.Statements.FileIO;

public record class InputStatement : FileStatement
{
    public InputStatement(WorkspaceUri parentUri, ValuedExpression fileNumber, IEnumerable<TypedSymbol> variables, LineLabelSymbol? parentLabel = null)
        : base(parentUri, fileNumber, parentLabel)
    {
        Variables = variables;
    }

    public IEnumerable<TypedSymbol> Variables { get; init; }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        // base implementation requires the file handle
        base.ExecuteInternal(context, rethrow);

        foreach (var symbol in Variables)
        {
            context.SetSymbolValue(symbol, VBEmptyValue.Empty.AsVariant());
        }

        return default;
    }
}
