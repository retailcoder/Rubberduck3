using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Types.Abstract;
using Rubberduck.InternalApi.Model.Symbols.Expressions;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions.Operators;

public abstract record class BinaryOperatorExpression<TValue> : OperatorExpression<TValue>
    where TValue : VBTypedValue
{
    protected BinaryOperatorExpression(string name, VBType vbType, WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(name, vbType, parentUri, [left, right])
    {
        Left = left;
        Right = right;
    }

    public ValuedExpression Left { get; }
    public ValuedExpression Right { get; }
}
