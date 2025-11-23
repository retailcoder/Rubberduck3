using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Types;
using Rubberduck.InternalApi.Model.Symbols.Values;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using System.Collections.Generic;

namespace Rubberduck.InternalApi.Model.Symbols.Abstract;

public record class EnumSymbol : TypedSymbol
{
    public EnumSymbol(string name, WorkspaceUri parentUri, Accessibility accessibility, IEnumerable<EnumMemberSymbol>? children = null, bool isUserDefined = false)
        : base(RubberduckSymbolKind.Enum, accessibility, name, parentUri, children)
    {
        Type = new VBEnumType(name, parentUri, this, isUserDefined: isUserDefined);
    }
}
