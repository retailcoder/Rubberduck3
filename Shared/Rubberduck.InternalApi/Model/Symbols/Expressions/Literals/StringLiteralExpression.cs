using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions.Literals;

public record class StringLiteralExpression : LiteralExpression<VBStringValue>
{
    public StringLiteralExpression(WorkspaceUri parentUri, VBStringValue value)
        : base(RubberduckSymbolKind.StringLiteral, parentUri, value)
    {
    }
}
