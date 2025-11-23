using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols;
using Rubberduck.InternalApi.Model.Symbols.Statements;

namespace Rubberduck.InternalApi.Model.Symbols.Statements.ControlFlow;

public record class ExitStatement : ExecutableStatement
{
    public ExitStatement(WorkspaceUri parentUri, BlockStatement? binding, LineLabelSymbol? parentLabel = null)
        : base(parentUri, parentLabel)
    {
        Binding = binding;
    }

    public BlockStatement? Binding { get; }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        if (Binding is not null)
        {
            context.CurrentScope.TryExitBlock(Binding);
        }

        return default;
    }
}
