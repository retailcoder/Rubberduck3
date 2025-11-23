using Rubberduck.InternalApi.Execution.Operators.Abstract;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model;
using Rubberduck.InternalApi.Model.Symbols.Expressions;

namespace Rubberduck.InternalApi.Execution.Operators;

public record class VBAdditionOperator : VBBinaryOperator
{
    public VBAdditionOperator(WorkspaceUri parentUri, ValuedExpression lhs, ValuedExpression rhs)
        : base(Tokens.AdditionOp, parentUri, lhs, rhs) { }

    protected override VBTypedValue ExecuteBinaryOperator(VBExecutionContext context, VBTypedValue lhsValue, VBTypedValue rhsValue) =>
        SymbolOperation.EvaluateBinaryOpResult(context, this, lhsValue, rhsValue, (lhs, rhs) => lhs + rhs);
}
