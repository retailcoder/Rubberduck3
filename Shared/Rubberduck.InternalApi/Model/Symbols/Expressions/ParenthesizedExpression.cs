using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols.Expressions.Operators;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions;

public record class ParenthesizedExpression : OperatorExpression<VBTypedValue>
{
    public ParenthesizedExpression(WorkspaceUri parentUri, ValuedExpression innerExpression)
        : base("LET_COERCE", innerExpression.Type!, parentUri, [innerExpression])
    {
        InnerExpression = innerExpression;
    }
    public ValuedExpression InnerExpression { get; }
    public override VBTypedValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        var value = InnerExpression.Execute(context, rethrow);
        if (value is VBObjectValue objectValue)
        {
            try
            {
                return objectValue.LetCoerce();
            }
            catch (VBRuntimeErrorException exception)
            {
                context.AddDiagnostics(exception);
                if (rethrow)
                {
                    throw;
                }
            }
        }
        else
        {
            return value;
        }

        return default;
    }
}
