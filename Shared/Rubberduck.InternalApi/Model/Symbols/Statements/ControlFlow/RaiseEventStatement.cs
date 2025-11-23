using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols;
using Rubberduck.InternalApi.Model.Symbols.Statements;

namespace Rubberduck.InternalApi.Model.Symbols.Statements.ControlFlow;

public record class RaiseEventStatement : ExecutableStatement
{
    public RaiseEventStatement(WorkspaceUri parentUri, LineLabelSymbol? parentLabel = null)
        : base(parentUri, parentLabel)
    {
    }
}
