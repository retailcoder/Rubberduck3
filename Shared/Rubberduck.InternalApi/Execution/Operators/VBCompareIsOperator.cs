using Rubberduck.InternalApi.Execution.Operators.Abstract;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model;
using Rubberduck.InternalApi.Model.Declarations.Types.Abstract;
using Rubberduck.InternalApi.Model.Symbols.Expressions;

namespace Rubberduck.InternalApi.Execution.Operators;

public record class VBCompareIsOperator : VBComparisonOperator
{
    public VBCompareIsOperator(WorkspaceUri parentUri, ValuedExpression lhs, ValuedExpression rhs)
        : base(Tokens.CompareIsOp, parentUri, lhs, rhs)
    {
    }

    protected override VBTypedValue ExecuteBinaryOperator(VBExecutionContext context, VBTypedValue lhsValue, VBTypedValue rhsValue)
    {
        if (lhsValue is not VBObjectValue lhsObj || lhsValue.TypeInfo is VBIntrinsicType)
        {
            throw VBRuntimeErrorException.TypeMismatch(lhsValue.Symbol!);
        }

        if (rhsValue is not VBObjectValue rhsObj || rhsValue.TypeInfo is VBIntrinsicType)
        {
            throw VBRuntimeErrorException.TypeMismatch(rhsValue.Symbol!);
        }

        return new VBBooleanValue(this) { Value = ReferenceEquals(lhsObj, rhsObj) };
    }
}
