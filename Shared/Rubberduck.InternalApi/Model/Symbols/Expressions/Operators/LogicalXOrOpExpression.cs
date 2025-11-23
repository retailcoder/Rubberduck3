using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols.Expressions;
using System;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions.Operators;

public record class LogicalXOrOpExpression : BitwiseOpExpression
{
    public LogicalXOrOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(parentUri, Tokens.LogicalXOrOp, left, right)
    {
    }

    protected override Func<int, int, int> BitwiseOp => (lhs, rhs) => lhs ^ rhs;
}
