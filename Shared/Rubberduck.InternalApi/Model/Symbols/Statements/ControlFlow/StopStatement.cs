using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols;
using Rubberduck.InternalApi.Model.Symbols.Statements;

namespace Rubberduck.InternalApi.Model.Symbols.Statements.ControlFlow;

public record class StopStatement : ExecutableStatement
{
    public StopStatement(WorkspaceUri parentUri, LineLabelSymbol? parentLabel = null)
        : base(parentUri, parentLabel)
    {
    }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        // diagnose?
        return base.ExecuteInternal(context, rethrow);
    }
}
