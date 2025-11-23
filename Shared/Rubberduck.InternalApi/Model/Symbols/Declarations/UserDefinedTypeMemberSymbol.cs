using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;

namespace Rubberduck.InternalApi.Model.Symbols.Abstract;

public record class UserDefinedTypeMemberSymbol : DeclarationExpressionSymbol
{
    public UserDefinedTypeMemberSymbol(string name, WorkspaceUri parentUri)
        : base(RubberduckSymbolKind.Field, name, parentUri, Accessibility.Public)
    {
    }
}
