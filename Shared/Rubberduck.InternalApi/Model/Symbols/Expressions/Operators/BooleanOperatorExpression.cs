using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Types;
using Rubberduck.InternalApi.Model.Symbols.Expressions;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions.Operators;

public abstract record class BooleanOperatorExpression : BinaryOperatorExpression<VBBooleanValue>
{
    protected BooleanOperatorExpression(string name, WorkspaceUri parentUri, ValuedExpression lhs, ValuedExpression rhs)
        : base(name, VBBooleanType.TypeInfo, parentUri, lhs, rhs)
    {
    }
}
