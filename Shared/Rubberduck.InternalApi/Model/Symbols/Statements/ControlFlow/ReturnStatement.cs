using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols;
using Rubberduck.InternalApi.Model.Symbols.Statements;

namespace Rubberduck.InternalApi.Model.Symbols.Statements.ControlFlow;

public record class ReturnStatement : ExecutableStatement
{
    public ReturnStatement(WorkspaceUri parentUri, LineLabelSymbol? parentLabel = null)
        : base(parentUri, parentLabel)
    {
    }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        if (!context.CurrentScope.TryReturn())
        {
            // TODO support error diagnostics for semantic tokens, I guess?
            //context.AddDiagnostics(VBRuntimeErrorException.ReturnWithoutGoSub(this));
        }

        return default;
    }
}
