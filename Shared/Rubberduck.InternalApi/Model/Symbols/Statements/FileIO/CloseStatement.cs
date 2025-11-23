using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols.Expressions;
using System.Collections.Generic;
using System.Linq;

namespace Rubberduck.InternalApi.Model.Symbols.Statements.FileIO;

public record class CloseStatement : ExecutableStatement
{
    public CloseStatement(WorkspaceUri parentUri, IEnumerable<ValuedExpression> fileNumbers, LineLabelSymbol? parentLabel = null)
        : base(parentUri, parentLabel)
    {
        FileNumbers = fileNumbers;
    }

    public IEnumerable<ValuedExpression> FileNumbers { get; init; } = [];

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        if (!FileNumbers.Any())
        {
            context.CloseFile();
        }
        else
        {
            foreach (var fileNumber in FileNumbers)
            {
                if (fileNumber.Execute(context, rethrow) is VBTypedValue handle)
                {
                    context.CloseFile(handle);
                }
            }
        }

        return default;
    }
}
