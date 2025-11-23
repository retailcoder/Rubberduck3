using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Types;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions.Operators;

public record class AddOpExpression : BinaryOperatorExpression<VBTypedValue>
{
    public AddOpExpression(WorkspaceUri parentUri, StringValuedExpression left, StringValuedExpression right)
        : base(Tokens.AdditionOp, VBStringType.TypeInfo, parentUri, left, right)
    {
    }
    public AddOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(Tokens.AdditionOp, VBDoubleType.TypeInfo, parentUri, left, right)
    {
    }

    public override VBTypedValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        VBTypedValue? result = default;

        var lhsValue = Left.Execute(context, rethrow);
        if (lhsValue is VBStringValue lhsString)
        {
            // if LHS coerces to a string, RHS is coerced to string and concatenated
            var rhsValue = Right.Execute(context, rethrow);
            if (rhsValue?.TypeInfo.ConvertsSafelyToType(VBStringType.TypeInfo) ?? false)
            {
                // TODO safely convert value to string

                if (rhsValue is VBStringValue rhsString)
                {
                    var resultValue = $"{lhsString.Value}{rhsString.Value}";
                    result = new VBStringValue(this) { Value = resultValue };
                }
            }
            else
            {
                // if we cannot coerce RHS to string, VBA would throw a type mismatch at runtime.
                var exception = VBRuntimeErrorException.TypeMismatch(this);
                context.AddDiagnostics(exception);
                if (rethrow)
                {
                    throw exception;
                }
            }
        }
        else if (lhsValue is VBNumericTypedValue lhsNumeric)
        {
            var rhsValue = Right.Execute(context, rethrow);
            if (rhsValue is VBNumericTypedValue rhsNumeric)
            {
                // both sides are numeric, perform addition
                var resultValue = lhsNumeric.NumericValue + rhsNumeric.NumericValue;

                // return the widest of the two types
                try
                {
                    result = lhsNumeric.Size >= rhsNumeric.Size
                        ? lhsNumeric.WithValue(resultValue)
                        : rhsNumeric.WithValue(resultValue);
                }
                catch (VBRuntimeErrorException vbRuntimeError)
                {
                    context.AddDiagnostics(vbRuntimeError);
                    if (rethrow)
                    {
                        throw;
                    }
                }
            }
            else
            {

                // RHS is not numeric, VBA would throw a type mismatch at runtime.
                var exception = VBRuntimeErrorException.TypeMismatch(this);
                context.AddDiagnostics(exception);
                if (rethrow)
                {
                    throw exception;
                }
            }
        }

        return result;
    }
}
