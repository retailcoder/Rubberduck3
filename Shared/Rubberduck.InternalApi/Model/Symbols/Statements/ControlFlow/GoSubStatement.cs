using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols;
using Rubberduck.InternalApi.Model.Symbols.Statements;

namespace Rubberduck.InternalApi.Model.Symbols.Statements.ControlFlow;

public record class GoSubStatement : ExecutableStatement
{
    public GoSubStatement(WorkspaceUri parentUri, LineLabelSymbol targetLabel, LineLabelSymbol? parentLabel = null)
        : base(parentUri, parentLabel)
    {
        TargetLabel = targetLabel;
    }

    public LineLabelSymbol TargetLabel { get; }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        if (!context.CurrentScope.TryGoSub(TargetLabel))
        {
            context.AddDiagnostics(VBCompileErrorException.LabelNotDefined(TargetLabel));
        }

        return default;
    }
}
