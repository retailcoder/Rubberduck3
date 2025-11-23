using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Types.Abstract;
using Rubberduck.InternalApi.Model.Symbols.Expressions;

namespace Rubberduck.InternalApi.Model.Symbols.Abstract;

public record class OptionalParameterSymbol : ParameterSymbol, IValuedSymbol
{
    public OptionalParameterSymbol(string name, WorkspaceUri parentUri, ParameterModifier modifier, VBType type, ValuedExpression? valueExpression = null)
        : base(name, parentUri, modifier, type)
    {
        ValueExpression = valueExpression;
    }

    public ValuedExpression? ValueExpression { get; init; }
    public VBTypedValue? Execute(VBExecutionContext context, bool rethrow = false) => ValueExpression?.Execute(context, rethrow);
}
