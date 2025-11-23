using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols;
using Rubberduck.InternalApi.Model.Symbols.Statements;

namespace Rubberduck.InternalApi.Model.Symbols.Statements.ErrorHandling;

public record class OnErrorGoToStatement : ExecutableStatement
{
    public OnErrorGoToStatement(WorkspaceUri parentUri, LineLabelSymbol target, LineLabelSymbol? parentLabel = null)
        : base(parentUri, parentLabel)
    {
        Target = target;
    }

    public LineLabelSymbol Target { get; init; }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        if (context.CurrentScope.ActiveErrorState)
        {
            // TODO diagnose bad error handling
        }

        context.CurrentScope.ActiveOnErrorResumeNext = false;
        context.CurrentScope.ActiveOnErrorGoTo = Target;
        return default;
    }
}
