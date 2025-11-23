using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols.Expressions;
using Rubberduck.InternalApi.Model.Symbols.Statements;
using System.Collections.Generic;

namespace Rubberduck.InternalApi.Model.Symbols.Statements.ControlFlow;

public record class WithStatementSymbol : BlockStatement
{
    public WithStatementSymbol(ObjectValuedExpression targetExpression, WorkspaceUri parentUri, IEnumerable<IExecutable>? children = null)
        : base(parentUri, children)
    {
        TargetExpression = targetExpression;
    }

    public ObjectValuedExpression TargetExpression { get; init; }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        if (TargetExpression.Execute(context, rethrow) is VBObjectValue withObject)
        {
            if (context.CurrentScope.TryEnterWithBlock(withObject))
            {
                try
                {
                    ExecuteBody(context, rethrow);
                }
                finally
                {
                    context.CurrentScope.TryExitWithBlock();
                }
            }
            else
            {
                context.AddDiagnostics(VBRuntimeErrorException.ObjectVariableNotSet(TargetExpression, "With block variable or expression evaluates to Nothing"));
            }
        }
        return default;
    }
}
