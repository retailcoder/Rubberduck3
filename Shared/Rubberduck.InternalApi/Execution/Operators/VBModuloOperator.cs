using Rubberduck.InternalApi.Execution.Operators.Abstract;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model;
using Rubberduck.InternalApi.Model.Symbols.Expressions;

namespace Rubberduck.InternalApi.Execution.Operators;

public record class VBModuloOperator : VBBinaryOperator
{
    public VBModuloOperator(WorkspaceUri parentUri, ValuedExpression lhs, ValuedExpression rhs)
        : base(Tokens.ModuloOp, parentUri, lhs, rhs)
    {
    }

    protected override VBTypedValue ExecuteBinaryOperator(VBExecutionContext context, VBTypedValue lhsValue, VBTypedValue rhsValue) =>
        SymbolOperation.EvaluateBinaryOpResult(context, this, lhsValue, rhsValue, (lhs, rhs) =>
        {
            if (rhs == 0)
            {
                throw VBRuntimeErrorException.DivisionByZero(this, "RHS/divisor operand must be non-zero. Consider validating the expression before using it as a divisor operand.");
            }
            return lhs % rhs;
        });
}
