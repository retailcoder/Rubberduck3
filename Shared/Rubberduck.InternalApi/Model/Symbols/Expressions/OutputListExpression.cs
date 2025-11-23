using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using System.Collections.Generic;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions;

public record class OutputListExpression : StringValuedExpression
{
    public OutputListExpression(WorkspaceUri parentUri, IEnumerable<ValuedExpression>? children = default)
        : base(parentUri, children ?? [])
    {

    }

    public override VBTypedValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        // TODO evaluate children, actually assemble an output string?
        return new VBStringValue(this) { Value = "(output list)" };
    }
}