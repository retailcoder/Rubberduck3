using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Symbols;

namespace Rubberduck.InternalApi.Model.Declarations.Operators.Abstract;

public abstract record class VBUnaryOperator : VBOperator
{
    protected VBUnaryOperator(string token, WorkspaceUri parentUri, ValuedExpression operand)
        : base(token, parentUri, [operand])
    {
        Operand = operand;
    }

    public ValuedExpression Operand { get; init; }
}
