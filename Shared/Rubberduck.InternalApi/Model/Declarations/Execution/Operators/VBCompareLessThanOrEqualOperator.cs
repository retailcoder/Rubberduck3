using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Execution;
using Rubberduck.InternalApi.Model.Declarations.Execution.Values;
using Rubberduck.InternalApi.Model.Declarations.Operators.Abstract;
using Rubberduck.InternalApi.Model.Declarations.Symbols;
using Rubberduck.InternalApi.Model.Declarations.Types;

namespace Rubberduck.InternalApi.Model.Declarations.Operators;

public record class VBCompareLessThanOrEqualOperator : VBComparisonOperator
{
    public VBCompareLessThanOrEqualOperator(WorkspaceUri parentUri, ValuedExpression lhs, ValuedExpression rhs)
        : base(Tokens.CompareLessThanOrEqualOp, parentUri, lhs, rhs)
    {
    }

    protected override VBTypedValue ExecuteBinaryOperator(VBExecutionContext context, VBTypedValue lhsValue, VBTypedValue rhsValue)
    {
        if (lhsValue.TypeInfo is VBStringType)
        {
            return SymbolOperation.ExecuteCompareOpResult(context, this, lhsValue, rhsValue,
                (lhs, rhs, comparison) => string.Compare(lhs, rhs, comparison) <= 0);
        }
        else
        {
            if (lhsValue is VBNumericTypedValue lhsNumeric)
            {
                return SymbolOperation.ExecuteCompareOpResult(context, this, lhsNumeric, rhsValue,
                    (lhs, rhs) => lhs.CompareTo(rhs) <= 0);
            }
        }
        throw VBRuntimeErrorException.TypeMismatch(this, $"Types {lhsValue.TypeInfo.Name} and {rhsValue.TypeInfo.Name} are not comparable.");
    }
}
