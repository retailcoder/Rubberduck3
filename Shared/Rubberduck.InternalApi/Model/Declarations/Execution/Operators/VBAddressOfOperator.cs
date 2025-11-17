using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Execution;
using Rubberduck.InternalApi.Model.Declarations.Execution.Values;
using Rubberduck.InternalApi.Model.Declarations.Operators.Abstract;
using Rubberduck.InternalApi.Model.Declarations.Symbols;

namespace Rubberduck.InternalApi.Model.Declarations.Operators;

public record class VBAddressOfOperator : VBUnaryOperator
{
    public VBAddressOfOperator(ValuedExpression operand, WorkspaceUri parentUri)
        : base(Tokens.AddressOf, parentUri, operand)
    {
    }

    protected override VBTypedValue? EvaluateResult(VBExecutionContext context) => context.CurrentScope.GetTypedValue(Operand);
}
