using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Types;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using System.Collections.Generic;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions;

public record class InvalidExpression : ValuedExpression
{
    public InvalidExpression(string name, WorkspaceUri parentUri, IEnumerable<ValuedExpression>? children = null)
        : base(RubberduckSymbolKind.UnknownSymbol, VBAnyType.TypeInfo, name, parentUri, children)
    {
    }
}
