using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Types;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using System.Collections.Generic;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions;

/// <summary>
/// Represents an expression that evaluates to an object reference.
/// </summary>
public record class ObjectValuedExpression : ValuedExpression<VBObjectValue>
{
    public ObjectValuedExpression(WorkspaceUri parentUri, IEnumerable<ValuedExpression>? children = null)
        : base(RubberduckSymbolKind.Expression, VBObjectType.TypeInfo, Tokens.Object, parentUri, children)
    {
    }
}
