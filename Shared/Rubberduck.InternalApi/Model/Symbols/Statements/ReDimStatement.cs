using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols;
using Rubberduck.InternalApi.Model.Symbols.Abstract;
using System.Collections.Generic;

namespace Rubberduck.InternalApi.Model.Symbols.Statements.Declarative;

public record class ReDimStatement : DimStatement
{
    public ReDimStatement(WorkspaceUri parentUri, IEnumerable<TypedSymbol> symbols, bool preserve = false, LineLabelSymbol? parentLabel = null)
        : base(parentUri, symbols, parentLabel)
    {
        Preserve = preserve;
    }

    public bool Preserve { get; init; }

    // NOTE: ReDim statements are declarative on scope entry, but still executable

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        foreach (var symbol in Symbols)
        {
            // TODO validate, issue diagnostics
            context.AddSymbol(symbol);
        }

        return default;
    }
}
