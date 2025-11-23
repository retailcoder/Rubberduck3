using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Types;
using Rubberduck.InternalApi.Model.Symbols.Expressions;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions.Operators;

public record class UnaryNotOpExpression : OperatorExpression<VBTypedValue>
{
    public UnaryNotOpExpression(WorkspaceUri parentUri, ValuedExpression expression)
        : base(Tokens.LogicalNotOp, VBLongType.TypeInfo, parentUri, [expression])
    {
        Expression = expression;
    }

    public ValuedExpression Expression { get; }

    public override VBTypedValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        VBTypedValue? result = default;

        var expression = Expression.Execute(context, rethrow);
        if (expression is VBBooleanValue boolExpression)
        {
            result = new VBBooleanValue(this) { Value = !boolExpression.Value };
        }
        else if (expression is VBNumericTypedValue numericExpression)
        {
            result = numericExpression.WithValue(-1 * numericExpression.NumericValue);
        }
        else
        {
            var exception = VBRuntimeErrorException.TypeMismatch(this);
            context.AddDiagnostics(exception);
            if (rethrow)
            {
                throw exception;
            }
        }

        return result;
    }
}
