using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols.Statements;
using System.Collections.Generic;

namespace Rubberduck.InternalApi.Model.Symbols.Statements.ControlFlow;

public record class ElseStatement : BlockStatement
{
    public ElseStatement(WorkspaceUri parentUri, IEnumerable<IExecutable> body)
        : base(parentUri, body) { }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        ExecuteBody(context, rethrow);
        return default;
    }
}
