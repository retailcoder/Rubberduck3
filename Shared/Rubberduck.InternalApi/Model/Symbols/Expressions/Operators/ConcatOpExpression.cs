using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Types;
using Rubberduck.InternalApi.Model.Symbols.Expressions;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions.Operators;

public record class ConcatOpExpression : OperatorExpression<VBStringValue>
{
    public ConcatOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(Tokens.ConcatOp, VBStringType.TypeInfo, parentUri, [left, right])
    {
        Left = left;
        Right = right;
    }
    public ValuedExpression Left { get; }
    public ValuedExpression Right { get; }

    public override VBStringValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        var rhs = Right.Execute(context, rethrow) as VBStringValue;
        var lhs = Left.Execute(context, rethrow) as VBStringValue;

        // TODO handle adding VBRuntimeErrorException.TypeMismatch to the execution context

        var result = $"{lhs?.Value}{rhs?.Value}";
        return new VBStringValue().WithValue(result);
    }
}
