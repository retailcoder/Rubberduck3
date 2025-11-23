using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols.Expressions;
using System;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions.Operators;

public record class LogicalEqvOpExpression : BitwiseOpExpression
{
    public LogicalEqvOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(parentUri, Tokens.LogicalEqvOp, left, right)
    {
    }
    protected override Func<int, int, int> BitwiseOp => (lhs, rhs) => ~(lhs ^ rhs);
}
