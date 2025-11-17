using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Execution;
using Rubberduck.InternalApi.Model.Declarations.Execution.Values;
using Rubberduck.InternalApi.Model.Declarations.Operators.Abstract;
using Rubberduck.InternalApi.Model.Declarations.Symbols;
using Rubberduck.InternalApi.Model.Declarations.Types.Abstract;

namespace Rubberduck.InternalApi.Model.Declarations.Operators;

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
