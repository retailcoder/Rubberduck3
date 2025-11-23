using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Types;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using System.Collections.Generic;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions;

public record class BooleanValuedExpression : ValuedExpression
{
    public BooleanValuedExpression(WorkspaceUri parentUri, IEnumerable<ValuedExpression>? children = null)
        : base(RubberduckSymbolKind.Expression, VBBooleanType.TypeInfo, string.Empty, parentUri, children)
    {
    }
}
