using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols.Expressions;
using System;
using System.Collections.Generic;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions.Operators;

public record class LikeCompareOpExpression : CompareOpExpression
{
    public LikeCompareOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(parentUri, Tokens.CompareLikeOp, left, right)
    {
    }
    protected override bool CompareNumberOp(double left, double right) => Comparer<double>.Default.Compare(left, right) >= 0;
    protected override bool CompareStringOp(string left, string right, StringComparison stringComparison) => string.Compare(left, right, stringComparison) >= 0;
}
