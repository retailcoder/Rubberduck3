using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Symbols;
using Rubberduck.InternalApi.Model.Declarations.Types;

namespace Rubberduck.InternalApi.Model.Declarations.Operators.Abstract;

public abstract record class VBComparisonOperator : VBBinaryOperator
{
    protected VBComparisonOperator(string token, WorkspaceUri parentUri, ValuedExpression lhs, ValuedExpression rhs)
        : base(token, parentUri, lhs, rhs)
    {
        Type = VBBooleanType.TypeInfo;
    }
}
