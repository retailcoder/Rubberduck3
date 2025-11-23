using Rubberduck.InternalApi.Execution.Operators.Abstract;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols.Expressions;
using System;

namespace Rubberduck.InternalApi.Execution.Operators;

public record class VBEqvOperator : VBBitwiseOperator
{
    public VBEqvOperator(string token, WorkspaceUri parentUri, ValuedExpression lhs, ValuedExpression rhs)
        : base(token, parentUri, lhs, rhs)
    {
    }

    protected override Func<int, int, int> BitwiseOp { get; } = (lhs, rhs) => ~(lhs ^ rhs);
}
