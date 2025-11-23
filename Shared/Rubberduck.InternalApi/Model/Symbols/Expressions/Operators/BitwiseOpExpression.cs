using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Types;
using Rubberduck.InternalApi.Model.Symbols.Expressions;
using System;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions.Operators;

public abstract record class BitwiseOpExpression : BinaryOperatorExpression<VBLongValue>
{
    public BitwiseOpExpression(WorkspaceUri parentUri, string op, ValuedExpression left, ValuedExpression right)
        : base(op, VBLongType.TypeInfo, parentUri, left, right)
    {
    }

    protected abstract Func<int, int, int> BitwiseOp { get; }

    public override VBLongValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        var lhs = Left.Execute(context, rethrow);
        var intLhs = 0;
        if (lhs is VBBooleanValue vbBoolL)
        {
            intLhs = vbBoolL.Value ? -1 : 0;
        }
        else if (lhs is VBNumericTypedValue vbNumL)
        {
            intLhs = (int)vbNumL.NumericValue;
        }

        var rhs = Right.Execute(context, rethrow);
        var intRhs = 0;
        if (rhs is VBBooleanValue vbBoolR)
        {
            intRhs = vbBoolR.Value ? -1 : 0;
        }
        else if (rhs is VBNumericTypedValue vbNumR)
        {
            intRhs = (int)vbNumR.NumericValue;
        }

        var result = BitwiseOp(intLhs, intRhs);
        return new VBLongValue().WithValue(result);
    }
}
