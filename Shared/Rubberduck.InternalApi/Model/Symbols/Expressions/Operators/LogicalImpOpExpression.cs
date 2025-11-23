using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols.Expressions;
using System;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions.Operators;

public record class LogicalImpOpExpression : BitwiseOpExpression
{
    public LogicalImpOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(parentUri, Tokens.LogicalImpOp, left, right)
    {
    }
    protected override Func<int, int, int> BitwiseOp => (lhs, rhs) => ~lhs | rhs;
}
