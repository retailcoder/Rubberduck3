using Rubberduck.InternalApi.Model.Declarations.Types.Abstract;

namespace Rubberduck.InternalApi.Model.Symbols.Abstract;

/// <summary>
/// Represents a symbol that can be resolved to a <c>VBType</c>.
/// </summary>
public interface ITypedSymbol
{
    VBType Type { get; }
}
