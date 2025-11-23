using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols.Abstract;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using System.Collections.Generic;

namespace Rubberduck.InternalApi.Model.Symbols.Declarations;

public record class ProjectSymbol : Symbol
{
    public ProjectSymbol(string name, WorkspaceUri workspaceUri, IEnumerable<Symbol> children)
        : base(RubberduckSymbolKind.Project, name, workspaceUri, Accessibility.Global, children)
    {
    }
}
