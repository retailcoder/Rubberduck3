using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Symbols;
using Rubberduck.InternalApi.Model.Declarations.Types;
using System.Collections.Generic;

namespace Rubberduck.InternalApi.Model.Declarations.Operators.Abstract;

public abstract record class VBOperator : OperatorSymbol
{
    protected VBOperator(string token, WorkspaceUri parentUri, IEnumerable<ValuedExpression>? operands = null)
        : base(token, VBVariantType.TypeInfo, parentUri, operands) { }
}
