using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Types.Abstract;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using System.Collections.Generic;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions.Operators;

public abstract record class OperatorSymbol : ValuedExpression<VBTypedValue>
{
    public OperatorSymbol(string token, VBType type, WorkspaceUri parentUri, IEnumerable<ValuedExpression>? children)
        : base(RubberduckSymbolKind.Operator, type, token, parentUri, children)
    {
    }

    public override VBTypedValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        try
        {
            return EvaluateResult(context);
        }
        catch (VBCompileErrorException vbCompileError)
        {
            context.AddDiagnostics(vbCompileError);
            if (rethrow)
            {
                throw;
            }
        }
        catch (VBRuntimeErrorException vbRuntimeError)
        {
            context.AddDiagnostics(vbRuntimeError);
            if (rethrow)
            {
                throw;
            }

        }
        return default;
    }

    protected abstract VBTypedValue? EvaluateResult(VBExecutionContext context);
}
