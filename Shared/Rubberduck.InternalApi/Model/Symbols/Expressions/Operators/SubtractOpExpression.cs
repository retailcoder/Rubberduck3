using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Types;
using Rubberduck.InternalApi.Model.Symbols.Expressions;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions.Operators;

public record class SubtractOpExpression : BinaryOperatorExpression<VBTypedValue>
{
    public SubtractOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(Tokens.SubtractionOp, VBDoubleType.TypeInfo, parentUri, left, right)
    {
    }

    public override VBTypedValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        VBTypedValue? result = default;

        var lhsValue = Left.Execute(context, rethrow);
        if (lhsValue is VBNumericTypedValue lhsNumeric)
        {
            var rhsValue = Right.Execute(context, rethrow);
            if (rhsValue is VBNumericTypedValue rhsNumeric)
            {
                // both sides are numeric, perform subtraction
                var resultValue = lhsNumeric.NumericValue - rhsNumeric.NumericValue;

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
