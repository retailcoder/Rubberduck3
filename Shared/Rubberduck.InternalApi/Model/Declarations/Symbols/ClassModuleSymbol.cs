using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Types;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using System.Collections.Generic;

namespace Rubberduck.InternalApi.Model.Declarations.Symbols;

public record class ClassModuleSymbol : TypedSymbol
{
    public ClassModuleSymbol(Instancing instancing, string name, WorkspaceUri fileUri, IEnumerable<Symbol>? children = null, bool predeclared = false, bool isUserDefined = false)
        : base(RubberduckSymbolKind.Class, instancing == Instancing.Private ? Accessibility.Private : Accessibility.Public, name, fileUri, children)
    {
        ResolvedType = new VBClassType(name, fileUri, isUserDefined: isUserDefined);
        Instancing = instancing;
        PredeclaredId = predeclared;
    }

    public Instancing Instancing { get; init; }
    public bool PredeclaredId { get; init; }
}
