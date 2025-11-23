using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Types;
using Rubberduck.InternalApi.Model.Symbols.Expressions;
using Rubberduck.InternalApi.Model.Symbols.Expressions.Operators;
using System.Collections.Generic;

namespace Rubberduck.InternalApi.Execution.Operators.Abstract;

public abstract record class VBOperator : OperatorSymbol
{
    protected VBOperator(string token, WorkspaceUri parentUri, IEnumerable<ValuedExpression>? operands = null)
        : base(token, VBVariantType.TypeInfo, parentUri, operands) { }
}
