using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols.Abstract;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using System.Collections.Generic;

namespace Rubberduck.InternalApi.Model.Symbols;

public record class InvalidSymbol : Symbol
{
    public InvalidSymbol(string name, WorkspaceUri? parentUri = null, IEnumerable<Symbol>? children = null)
        : base(RubberduckSymbolKind.UnknownSymbol, name, parentUri, children: children)
    {
    }
}
