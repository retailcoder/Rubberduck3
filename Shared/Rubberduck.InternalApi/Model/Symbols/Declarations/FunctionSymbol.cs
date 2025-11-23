using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Types.Abstract;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using System.Collections.Generic;

namespace Rubberduck.InternalApi.Model.Symbols.Abstract;

public record class FunctionSymbol : TypedSymbol, IExecutable
{
    public FunctionSymbol(string name, WorkspaceUri parentUri, Accessibility accessibility, VBType type, IEnumerable<Symbol>? children = null, RubberduckSymbolKind kind = RubberduckSymbolKind.Function)
        : base(kind, accessibility, name, type, parentUri, children ?? [])
    {
    }

    public VBTypedValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        var member = context.GetModuleMember(this);
        if (member != null)
        {
            var scope = context.EnterScope(member);
            scope.Execute(context, rethrow);

        }
        throw VBRuntimeErrorException.PropertyOrMethodNotFound(this); // fitting, but is it really a VB-trappable error? or it's a .net-side bug?
    }
}
