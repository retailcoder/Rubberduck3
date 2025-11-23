using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Operators.Abstract;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model;
using Rubberduck.InternalApi.Model.Symbols.Expressions;

namespace Rubberduck.InternalApi.Execution.Operators;

public record class VBAddressOfOperator : VBUnaryOperator
{
    public VBAddressOfOperator(ValuedExpression operand, WorkspaceUri parentUri)
        : base(Tokens.AddressOf, parentUri, operand)
    {
    }

    protected override VBTypedValue? EvaluateResult(VBExecutionContext context) => context.CurrentScope.GetTypedValue(Operand);
}
