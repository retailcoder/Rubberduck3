using Rubberduck.InternalApi.Model.Declarations.Types.Abstract;
using Rubberduck.InternalApi.Model.Symbols.Abstract;

namespace Rubberduck.Parsing._v3.Pipeline;

public interface IResolverService
{
    VBType? Resolve(TypedSymbol symbol);
}
