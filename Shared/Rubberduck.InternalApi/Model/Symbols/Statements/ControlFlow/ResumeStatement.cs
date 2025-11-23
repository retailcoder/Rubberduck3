using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;

namespace Rubberduck.InternalApi.Model.Symbols.Statements.ErrorHandling;

public record class ResumeStatement : ExecutableStatement
{
    public ResumeStatement(WorkspaceUri parentUri, LineLabelSymbol? target, LineLabelSymbol? parentLabel = null)
        : base(parentUri, parentLabel)
    {
        Target = target;
    }

    public LineLabelSymbol? Target { get; init; }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        if (!context.CurrentScope.ActiveErrorState)
        {
            //var exception = VBRuntimeErrorException.ResumeWithoutError(this);
            //context.AddDiagnostics(exception);
            //if (rethrow)
            //{
            //    throw exception;
            //}
        }
        else if (Target != null)
        {
            context.CurrentScope.TryResume(Target);
        }
        else
        {
            context.CurrentScope.TryResume();
        }

        return default;
    }
}