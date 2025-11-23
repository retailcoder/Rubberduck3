using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Types;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using System.Collections.Generic;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions;

/// <summary>
/// Represents an expression that evaluates to a string value.
/// </summary>
public record class StringValuedExpression : ValuedExpression
{
    public StringValuedExpression(WorkspaceUri parentUri, IEnumerable<ValuedExpression>? children = null)
        : base(RubberduckSymbolKind.Expression, VBStringType.TypeInfo, string.Empty, parentUri, children)
    {
    }
}
