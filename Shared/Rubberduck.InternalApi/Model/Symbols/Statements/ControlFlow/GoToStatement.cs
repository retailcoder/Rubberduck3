using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols;
using Rubberduck.InternalApi.Model.Symbols.Statements;

namespace Rubberduck.InternalApi.Model.Symbols.Statements.ControlFlow;

public record class GoToStatement : ExecutableStatement
{
    public GoToStatement(WorkspaceUri parentUri, LineLabelSymbol targetLabel, bool isImplicit = false, LineLabelSymbol? parentLabel = default)
        : base(parentUri, parentLabel)
    {
        TargetLabel = targetLabel;
        IsImplicit = isImplicit;
    }

    public bool IsImplicit { get; init; }
    public LineLabelSymbol TargetLabel { get; }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        if (!context.CurrentScope.TryGoTo(TargetLabel))
        {
            context.AddDiagnostics(VBCompileErrorException.LabelNotDefined(TargetLabel));
        }

        return default;
    }
}
