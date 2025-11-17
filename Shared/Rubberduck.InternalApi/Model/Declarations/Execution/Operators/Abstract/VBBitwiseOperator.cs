using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Execution;
using Rubberduck.InternalApi.Model.Declarations.Execution.Values;
using Rubberduck.InternalApi.Model.Declarations.Symbols;
using Rubberduck.InternalApi.Model.Declarations.Types;
using System;

namespace Rubberduck.InternalApi.Model.Declarations.Operators.Abstract;

public abstract record class VBBitwiseOperator : VBBinaryOperator
{
    protected VBBitwiseOperator(string token, WorkspaceUri parentUri, ValuedExpression lhs, ValuedExpression rhs)
        : base(token, parentUri, lhs, rhs)
    {
        Type = VBLongType.TypeInfo;
    }

    public VBBooleanValue ExecuteAsLogicalOp(VBExecutionContext context, VBTypedValue lhsValue, VBTypedValue rhsValue) =>
        new(this) { Value = ((VBLongValue)ExecuteBinaryOperator(context, lhsValue, rhsValue)).Value != 0 };

    protected abstract Func<int, int, int> BitwiseOp { get; }

    protected sealed override VBTypedValue ExecuteBinaryOperator(VBExecutionContext context, VBTypedValue lhsValue, VBTypedValue rhsValue)
    {
        var result = SymbolOperation.EvaluateBinaryOpResult(context, this, lhsValue, rhsValue, BitwiseOp);
        if (lhsValue.TypeInfo is VBBooleanType && rhsValue.TypeInfo is VBBooleanType)
        {
            return result;
        }

        context.AddDiagnostic(RubberduckDiagnostic.BitwiseOperator(this));
        return ((INumericValue)result).AsLong();
    }
}
