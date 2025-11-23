using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols.Expressions;
using Rubberduck.InternalApi.Model.Symbols.Statements;
using System.Collections.Generic;

namespace Rubberduck.InternalApi.Model.Symbols.Statements.ControlFlow;

public record class WhileWendStatement : BlockStatement
{
    public WhileWendStatement(BooleanValuedExpression condition, WorkspaceUri parentUri, IEnumerable<IExecutable>? children = null)
        : base(parentUri, children)
    {
        Condition = condition;
    }

    public BooleanValuedExpression Condition { get; init; }
}
