using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Types;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using System.Collections.Generic;
using System.Linq;

namespace Rubberduck.InternalApi.Model.Symbols.Abstract;

public record class UserDefinedTypeSymbol : TypedSymbol
{
    public UserDefinedTypeSymbol(string name, WorkspaceUri parentUri, Accessibility accessibility, IEnumerable<UserDefinedTypeMemberSymbol> children)
        : base(RubberduckSymbolKind.UserDefinedType, accessibility, name, parentUri, children.Cast<Symbol>())
    {
        Type = new VBUserDefinedType(name, parentUri, this);
    }
}
