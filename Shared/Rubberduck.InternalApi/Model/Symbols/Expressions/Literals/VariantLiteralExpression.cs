using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions.Literals;

public abstract record class VariantLiteralExpression : LiteralExpression<VBVariantValue>
{
    protected VariantLiteralExpression(WorkspaceUri parentUri, VBVariantValue value)
        : base(RubberduckSymbolKind.VariantLiteral, parentUri, value)
    {
    }
}
