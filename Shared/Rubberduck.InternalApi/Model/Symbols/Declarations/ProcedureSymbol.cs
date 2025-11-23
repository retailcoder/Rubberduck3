using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Types;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using System.Collections.Generic;
using System.Linq;

namespace Rubberduck.InternalApi.Model.Symbols.Abstract;

public record class ProcedureSymbol : TypedSymbol, IExecutable
{
    public ProcedureSymbol(string name, WorkspaceUri parentUri, Accessibility accessibility, IEnumerable<Symbol>? children = null, RubberduckSymbolKind kind = RubberduckSymbolKind.Procedure)
        : base(kind, accessibility, name, parentUri, (children ?? []).ToArray(), VBLongPtrType.TypeInfo) { }

    public VBTypedValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        var member = context.GetModuleMember(this);
        if (member != null)
        {
            try
            {
                var scope = context.EnterScope(member);
                scope.Execute(context);
            }
            catch (VBCompileErrorException vbCompileError)
            {
                context.Diagnostics.Add(vbCompileError.Diagnostic);
                context.End();
            }
            catch (VBRuntimeErrorException vbRuntimeError)
            {
                // unhandled runtime error
                context.Diagnostics.Add(vbRuntimeError.Diagnostic);
            }
            return null;
        }
        throw VBRuntimeErrorException.PropertyOrMethodNotFound(this); // fitting, but is it really a VB-trappable error? or it's a .net-side bug?
    }
}
