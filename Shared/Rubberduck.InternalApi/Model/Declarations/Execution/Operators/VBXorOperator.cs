using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Operators.Abstract;
using Rubberduck.InternalApi.Model.Declarations.Symbols;
using System;

namespace Rubberduck.InternalApi.Model.Declarations.Operators;

public record class VBXorOperator : VBBitwiseOperator
{
    public VBXorOperator(WorkspaceUri parentUri, ValuedExpression lhs, ValuedExpression rhs)
        : base(Tokens.LogicalXOrOp, parentUri, lhs, rhs)
    {
    }

    protected override Func<int, int, int> BitwiseOp { get; } = (lhs, rhs) => lhs ^ rhs;
}
