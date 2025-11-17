using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Execution;
using Rubberduck.InternalApi.Model.Declarations.Execution.Values;
using Rubberduck.InternalApi.Model.Declarations.Operators.Abstract;
using Rubberduck.InternalApi.Model.Declarations.Symbols;
using System;

namespace Rubberduck.InternalApi.Model.Declarations.Operators;

public record class VBIntegerDivisionOperator : VBBinaryOperator
{
    public VBIntegerDivisionOperator(WorkspaceUri parentUri, ValuedExpression lhs, ValuedExpression rhs)
        : base(Tokens.IntegerDivisionOp, parentUri, lhs, rhs) { }

    protected override VBTypedValue ExecuteBinaryOperator(VBExecutionContext context, VBTypedValue lhsValue, VBTypedValue rhsValue) =>
        SymbolOperation.EvaluateBinaryOpResult(context, this, lhsValue, rhsValue, (lhs, rhs) =>
        {
            if (rhs == 0)
            {
                throw VBRuntimeErrorException.DivisionByZero(this, "RHS/divisor operand must be non-zero. Consider validating the expression before using it as a divisor operand.");
            }

            if (rhs <= 0.5)
            {
                throw VBRuntimeErrorException.DivisionByZero(this, "The rounded integer value of the RHS/divisor operand must be non-zero. Consider validating the expression before using it as a divisor operand.");
            }

            return Math.Round(lhs / rhs, 0, MidpointRounding.ToZero);
        });
}
