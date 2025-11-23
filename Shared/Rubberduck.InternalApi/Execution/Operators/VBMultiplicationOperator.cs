using Rubberduck.InternalApi.Execution.Operators.Abstract;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model;
using Rubberduck.InternalApi.Model.Symbols.Expressions;

namespace Rubberduck.InternalApi.Execution.Operators;

public record class VBMultiplicationOperator : VBBinaryOperator
{
    public VBMultiplicationOperator(WorkspaceUri parentUri, ValuedExpression lhs, ValuedExpression rhs)
        : base(Tokens.MultiplicationOp, parentUri, lhs, rhs)
    {
    }

    protected override VBTypedValue ExecuteBinaryOperator(VBExecutionContext context, VBTypedValue lhsValue, VBTypedValue rhsValue) =>
        rhsValue is VBDateValue rhsDateValue
        ? SymbolOperation.EvaluateBinaryOpResult(context, this, lhsValue, rhsValue, (double lhs, double rhs) => lhs * rhsDateValue.SerialValue)
        : SymbolOperation.EvaluateBinaryOpResult(context, this, lhsValue, rhsValue, (lhs, rhs) => lhs * rhs);
}
