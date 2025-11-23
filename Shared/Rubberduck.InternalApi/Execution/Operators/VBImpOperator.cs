using Rubberduck.InternalApi.Execution.Operators.Abstract;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model;
using Rubberduck.InternalApi.Model.Symbols.Expressions;
using System;

namespace Rubberduck.InternalApi.Execution.Operators;

public record class VBImpOperator : VBBitwiseOperator
{
    public VBImpOperator(string token, WorkspaceUri parentUri, ValuedExpression lhs, ValuedExpression rhs)
        : base(Tokens.LogicalImpOp, parentUri, lhs, rhs)
    {
    }

    protected override Func<int, int, int> BitwiseOp { get; } = (lhs, rhs) => ~lhs | rhs;
}