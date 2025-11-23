using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations;
using Rubberduck.InternalApi.Model.Declarations.Types.Abstract;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using System.Collections.Generic;

namespace Rubberduck.InternalApi.Model.Symbols.Abstract;

public abstract record class DeclarationExpressionSymbol : TypedSymbol
{
    protected DeclarationExpressionSymbol(RubberduckSymbolKind kind, string name, WorkspaceUri parentUri, Accessibility accessibility, VBType type, IEnumerable<Symbol>? children = null, IEnumerable<IParseTreeAnnotation>? annotations = null)
        : base(kind, accessibility, name, parentUri, children, type)
    {
    }
}
