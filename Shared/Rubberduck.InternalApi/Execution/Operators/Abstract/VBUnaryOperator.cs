using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols.Expressions;

namespace Rubberduck.InternalApi.Execution.Operators.Abstract;

public abstract record class VBUnaryOperator : VBOperator
{
    protected VBUnaryOperator(string token, WorkspaceUri parentUri, ValuedExpression operand)
        : base(token, parentUri, [operand])
    {
        Operand = operand;
    }

    public ValuedExpression Operand { get; init; }
}
