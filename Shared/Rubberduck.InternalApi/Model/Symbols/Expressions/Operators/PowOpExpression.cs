using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Types;
using Rubberduck.InternalApi.Model.Symbols.Expressions;
using System;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions.Operators;

public record class PowOpExpression : BinaryOperatorExpression<VBNumericTypedValue>
{
    public PowOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(Tokens.PowerOp, VBDoubleType.TypeInfo, parentUri, left, right)
    {
    }

    public override VBNumericTypedValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        VBNumericTypedValue? result = default;

        var rhsValue = Right.Execute(context, rethrow);
        var lhsValue = Left.Execute(context, rethrow);

        if (lhsValue is VBNumericTypedValue lhsNumeric)
        {
            if (rhsValue is VBNumericTypedValue rhsNumeric)
            {
                // both sides are numeric, perform subtraction
                var resultValue = Math.Pow(lhsNumeric.NumericValue, rhsNumeric.NumericValue);

                // return the widest of the two types
                try
                {
                    result = (lhsNumeric.Size >= rhsNumeric.Size
                        ? lhsNumeric.WithValue(resultValue)
                        : rhsNumeric.WithValue(resultValue)) as VBNumericTypedValue;
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
                if (rethrow)
                {
                    throw exception;
                }
            }
        }

        return result;
    }
}
