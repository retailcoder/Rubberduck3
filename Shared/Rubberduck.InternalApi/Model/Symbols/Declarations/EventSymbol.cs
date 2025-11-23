using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using System.Collections.Generic;

namespace Rubberduck.InternalApi.Model.Symbols.Abstract;

public record class EventSymbol : ProcedureSymbol
{
    public EventSymbol(string name, WorkspaceUri parentUri, Accessibility accessibility, IEnumerable<ParameterSymbol>? parameters = default)
        : base(name, parentUri, accessibility, parameters, RubberduckSymbolKind.Event) { }
}
