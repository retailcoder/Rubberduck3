using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Execution;
using Rubberduck.InternalApi.Model.Declarations.Execution.Values;
using Rubberduck.InternalApi.Model.Declarations.Operators.Abstract;
using Rubberduck.InternalApi.Model.Declarations.Symbols;

namespace Rubberduck.InternalApi.Model.Declarations.Operators;

public record class VBMultiplicationOperator : VBBinaryOperator
{
    public VBMultiplicationOperator(WorkspaceUri parentUri, ValuedExpression lhs, ValuedExpression rhs)
        : base(Tokens.MultiplicationOp, parentUri, lhs, rhs)
    {
    }

    protected override VBTypedValue ExecuteBinaryOperator(VBExecutionContext context, VBTypedValue lhsValue, VBTypedValue rhsValue) =>
        rhsValue is VBDateValue rhsDateValue
        ? SymbolOperation.EvaluateBinaryOpResult(context, this, lhsValue, rhsValue, (double lhs, double rhs) => lhs * rhsDateValue.SerialValue)
        : SymbolOperation.EvaluateBinaryOpResult(context, this, lhsValue, rhsValue, (int lhs, int rhs) => lhs * rhs);
}
