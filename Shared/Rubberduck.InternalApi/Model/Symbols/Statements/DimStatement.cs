using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols;
using Rubberduck.InternalApi.Model.Symbols.Abstract;
using Rubberduck.InternalApi.Model.Symbols.Statements;
using System.Collections.Generic;

namespace Rubberduck.InternalApi.Model.Symbols.Statements.Declarative;

public record class DimStatement : ExecutableStatement
{
    // NOTE: Dim statements are executed upon entering a scope

    public DimStatement(WorkspaceUri parentUri, IEnumerable<TypedSymbol> symbols, LineLabelSymbol? parentLabel = null)
        : base(parentUri, parentLabel)
    {
        Symbols = symbols;
    }

    public IEnumerable<TypedSymbol> Symbols { get; }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        foreach (var symbol in Symbols)
        {
            context.AddSymbol(symbol);
        }

        return default;
    }
}
