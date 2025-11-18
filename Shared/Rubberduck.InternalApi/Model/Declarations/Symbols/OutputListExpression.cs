using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Execution;
using Rubberduck.InternalApi.Model.Declarations.Execution.Values;
using System.Collections.Generic;

namespace Rubberduck.InternalApi.Model.Declarations.Symbols;

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