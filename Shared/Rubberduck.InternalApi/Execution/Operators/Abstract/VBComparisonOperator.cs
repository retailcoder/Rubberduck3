using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Types;
using Rubberduck.InternalApi.Model.Symbols.Expressions;

namespace Rubberduck.InternalApi.Execution.Operators.Abstract;

public abstract record class VBComparisonOperator : VBBinaryOperator
{
    protected VBComparisonOperator(string token, WorkspaceUri parentUri, ValuedExpression lhs, ValuedExpression rhs)
        : base(token, parentUri, lhs, rhs)
    {
        Type = VBBooleanType.TypeInfo;
    }
}
