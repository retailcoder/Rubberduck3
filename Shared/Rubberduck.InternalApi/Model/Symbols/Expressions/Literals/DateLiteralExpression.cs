using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions.Literals;

public record class DateLiteralExpression : LiteralExpression<VBDateValue>
{
    public DateLiteralExpression(WorkspaceUri parentUri, VBDateValue value)
        : base(RubberduckSymbolKind.DateLiteral, parentUri, value)
    {
    }
}
