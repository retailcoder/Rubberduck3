using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols.Abstract;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;

namespace Rubberduck.InternalApi.Model.Symbols;

public record class LineLabelSymbol : Symbol
{
    public LineLabelSymbol(string name, WorkspaceUri parentUri)
        : base(RubberduckSymbolKind.LineLabel, name, parentUri)
    {
    }
    public bool IsLineNumber => int.TryParse(Name, out _);
}
