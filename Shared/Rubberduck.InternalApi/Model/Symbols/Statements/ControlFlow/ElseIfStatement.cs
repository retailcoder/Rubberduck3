using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols.Expressions;
using Rubberduck.InternalApi.Model.Symbols.Statements;
using System.Collections.Generic;

namespace Rubberduck.InternalApi.Model.Symbols.Statements.ControlFlow;

public record class ElseIfStatement : BlockStatement
{
    public ElseIfStatement(WorkspaceUri parentUri, BooleanValuedExpression condition, IEnumerable<IExecutable> body)
        : base(parentUri, body)
    {
        Condition = condition;
    }

    public BooleanValuedExpression Condition { get; }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        var condition = Condition.Execute(context, rethrow);
        if (condition is VBBooleanValue conditionResult)
        {
            if (conditionResult.Value)
            {
                ExecuteBody(context, rethrow);
                return VBBooleanValue.True;
            }
            else
            {
                return VBBooleanValue.False;
            }
        }

        return default;
    }
}
