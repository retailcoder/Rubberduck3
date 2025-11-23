using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Model.Symbols.Expressions;

namespace Rubberduck.InternalApi.Model.Symbols.Abstract;

/// <summary>
/// Represents a typed symbol that defines a value expression that can be resolved to a <c>VBType</c>.
/// </summary>
public interface IValuedSymbol : ITypedSymbol, IExecutable<VBTypedValue>
{
    /// <summary>
    /// The declared value expression, if present.
    /// </summary>
    ValuedExpression? ValueExpression { get; }
}
