using Rubberduck.InternalApi.Execution.Operators.Abstract;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model;
using Rubberduck.InternalApi.Model.Symbols.Expressions;
using System;

namespace Rubberduck.InternalApi.Execution.Operators;

public record class VBPowerOperator : VBBinaryOperator
{
    public VBPowerOperator(WorkspaceUri parentUri, ValuedExpression lhs, ValuedExpression rhs)
        : base(Tokens.PowerOp, parentUri, lhs, rhs)
    {
    }

    protected override VBTypedValue ExecuteBinaryOperator(VBExecutionContext context, VBTypedValue lhsValue, VBTypedValue rhsValue) =>
        SymbolOperation.EvaluateBinaryOpResult(context, this, lhsValue, rhsValue, (double lhs, double rhs) => Math.Pow(lhs, rhs));
}
