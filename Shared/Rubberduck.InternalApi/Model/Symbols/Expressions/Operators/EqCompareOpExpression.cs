using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols.Expressions;
using System;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions.Operators;

public record class EqCompareOpExpression : CompareOpExpression
{
    public EqCompareOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(parentUri, Tokens.CompareEqualOp, left, right)
    {
    }

    protected override bool CompareNumberOp(double left, double right) => left.Equals(right);

    protected override bool CompareStringOp(string left, string right, StringComparison stringComparison) => left.Equals(right, stringComparison);
}
