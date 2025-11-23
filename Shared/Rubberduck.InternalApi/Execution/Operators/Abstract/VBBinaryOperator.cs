using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols.Expressions;
using System;
using System.Linq;

namespace Rubberduck.InternalApi.Execution.Operators.Abstract;

public abstract record class VBBinaryOperator : VBOperator
{
    protected VBBinaryOperator(string token, WorkspaceUri parentUri, ValuedExpression lhs, ValuedExpression rhs)
        : base(token, parentUri, [lhs, rhs])
    {
        Left = lhs;
        Right = rhs;
    }

    public ValuedExpression Left { get; init; }
    public ValuedExpression Right { get; init; }

    protected sealed override VBTypedValue? EvaluateResult(VBExecutionContext context)
    {
        var lhs = Left.Execute(context);
        var rhs = Right.Execute(context);

        if (lhs != null && rhs != null)
        {
            return ExecuteBinaryOperator(context, lhs, rhs);
        }

        return default;
    }

    protected abstract VBTypedValue ExecuteBinaryOperator(VBExecutionContext context, VBTypedValue lhsValue, VBTypedValue rhsValue);

    protected bool CanConvertSafely(VBTypedValue lhsValue, VBTypedValue rhsValue)
        => lhsValue.TypeInfo.ConvertsSafelyToTypes.Contains(rhsValue.TypeInfo);
}
