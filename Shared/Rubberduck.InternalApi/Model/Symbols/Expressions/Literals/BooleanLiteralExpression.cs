using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions.Literals;

public record class BooleanLiteralExpression : LiteralExpression<VBBooleanValue>
{
    public BooleanLiteralExpression(WorkspaceUri parentUri, VBBooleanValue value)
        : base(RubberduckSymbolKind.BooleanLiteral, parentUri, value)
    {
    }
}
