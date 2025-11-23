using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using System.Collections.Generic;
using System.Linq;

namespace Rubberduck.InternalApi.Model.Symbols.Abstract;

public record class PropertySetSymbol : ProcedureSymbol
{
    public PropertySetSymbol(string name, WorkspaceUri parentUri, Accessibility accessibility, IEnumerable<Symbol>? children = null)
        : base(name, parentUri, accessibility, (children ?? []).ToArray(), RubberduckSymbolKind.Property) { }
}
