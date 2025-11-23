using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Operators;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols.Expressions;
using System;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions.Operators;

public abstract record class CompareOpExpression : BooleanOperatorExpression
{
    public CompareOpExpression(WorkspaceUri parentUri, string op, ValuedExpression left, ValuedExpression right)
        : base(op, parentUri, left, right)
    {
    }

    protected abstract bool CompareStringOp(string left, string right, StringComparison stringComparison);
    protected abstract bool CompareNumberOp(double left, double right);

    public override VBBooleanValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        VBBooleanValue? result = default;

        var rhsValue = Right.Execute(context, rethrow);
        var lhsValue = Left.Execute(context, rethrow);

        if (lhsValue is VBNumericTypedValue lhsNumeric)
        {
            if (rhsValue is VBNumericTypedValue rhsNumeric)
            {
                result = SymbolOperation.ExecuteCompareOpResult(context, this, lhsNumeric, rhsNumeric, CompareNumberOp);
            }
            else if (rhsValue?.TypeInfo.ConvertsSafelyToType(lhsValue.TypeInfo) ?? false)
            {
                var coercedRhs = rhsValue.AsVariant().AsCoercedNumeric()?.NumericValue;
                result = SymbolOperation.ExecuteCompareOpResult(context, this, lhsNumeric, rhsValue, CompareNumberOp);
            }
            else
            {
                var exception = VBRuntimeErrorException.TypeMismatch(Right);
                context.AddDiagnostics(exception);
                if (rethrow)
                {
                    throw exception;
                }
            }
        }
        else if (lhsValue is VBStringValue lhsString)
        {
            if (rhsValue is VBStringValue rhsString)
            {
                result = SymbolOperation.ExecuteCompareOpResult(context, this, lhsString, rhsString, CompareStringOp);
            }
            else if (rhsValue?.TypeInfo.ConvertsSafelyToType(lhsValue.TypeInfo) ?? false)
            {
                var coercedRhs = rhsValue.AsVariant().AsCoercedString();
                result = SymbolOperation.ExecuteCompareOpResult(context, this, lhsString, rhsValue, CompareStringOp);
            }
        }

        return result;
    }
}
