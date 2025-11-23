using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions.Literals;

/// <summary>
/// Represents the <c>Nothing</c> literal value.
/// </summary>
public record class ObjectLiteralExpression : LiteralExpression<VBObjectValue>
{
    public ObjectLiteralExpression(WorkspaceUri parentUri)
        : base(RubberduckSymbolKind.Nothing, parentUri, VBObjectValue.Nothing)
    {
    }
}
