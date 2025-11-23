using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Types;
using Rubberduck.InternalApi.Model.Declarations.Types.Abstract;
using Rubberduck.InternalApi.Model.Symbols.Expressions;
using System;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions.Operators;

public record class IsCompareOpExpression : CompareOpExpression
{
    public IsCompareOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(parentUri, Tokens.CompareIsOp, left, right)
    {
    }

    // NOTE: there would be a type mismatch on LHS symbol given a VBObjectValue RHS
    // These overrides are obligatory, but will not be invoked since we're overriding Execute

    protected override bool CompareStringOp(string left, string right, StringComparison stringComparison) => throw VBCompileErrorException.TypeMismatch(Right, "An object reference is expected in this context");

    protected override bool CompareNumberOp(double left, double right) => throw VBCompileErrorException.TypeMismatch(Right, "An object reference is expected in this context");

    public override VBBooleanValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        var rhs = Right.Execute(context, rethrow);
        var lhs = Left.Execute(context, rethrow);

        if (rhs?.TypeInfo is VBTypeDesc typeofRhs)
        {
            if (lhs?.TypeInfo is VBTypeDesc typeofLhs)
            {
                var result = typeofLhs.Equals(typeofRhs);
                return new VBBooleanValue(this) { Value = result };
            }
            else
            {
                throw VBCompileErrorException.TypeMismatch(Left);
            }

        }
        else if (Right.Type is VBObjectType)
        {
            throw VBCompileErrorException.TypeMismatch(Right);
        }

        // TODO
        throw new NotImplementedException();

        //return base.Execute(context, rethrow);
    }
}
