using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Types.Abstract;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using System.Collections.Generic;

namespace Rubberduck.InternalApi.Model.Symbols.Abstract;

public record class PropertyGetSymbol : FunctionSymbol
{
    public PropertyGetSymbol(string name, WorkspaceUri parentUri, Accessibility accessibility, VBType type, IEnumerable<Symbol>? children = null)
        : base(name, parentUri, accessibility, type, children, RubberduckSymbolKind.Property)
    {
    }
}
