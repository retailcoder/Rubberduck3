using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols.Expressions;
using Rubberduck.InternalApi.Model.Symbols.Statements;
using System.Collections.Generic;

namespace Rubberduck.InternalApi.Model.Symbols.Statements.ControlFlow;

public record class IfStatement : BlockStatement
{
    public IfStatement(WorkspaceUri parentUri, BooleanValuedExpression condition, IEnumerable<IExecutable> body, IEnumerable<ElseIfStatement> elseIfBlocks = default, ElseStatement? elseBlock = default)
        : base(parentUri, body)
    {
        Condition = condition;
        ElseIfBlocks = elseIfBlocks ?? [];
        ElseBlock = elseBlock;
    }

    public BooleanValuedExpression Condition { get; }

    public IEnumerable<ElseIfStatement> ElseIfBlocks { get; } = [];
    public ElseStatement? ElseBlock { get; init; }

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
                foreach (var elseIfBlock in ElseIfBlocks)
                {
                    if (elseIfBlock.Execute(context, rethrow) is VBBooleanValue didExecute && didExecute.Value)
                    {
                        break;
                    }
                }

                ElseBlock?.Execute(context, rethrow);
                return VBBooleanValue.False;
            }
        }

        return default;
    }
}
