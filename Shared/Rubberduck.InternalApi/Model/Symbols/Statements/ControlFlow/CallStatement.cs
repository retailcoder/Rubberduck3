using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Types.Abstract;
using Rubberduck.InternalApi.Model.Symbols.Expressions;
using Rubberduck.InternalApi.Model.Symbols.Statements;
using System;
using System.Linq;

namespace Rubberduck.InternalApi.Model.Symbols.Statements.ControlFlow;

/// <summary>
/// Represents an executable call statement.
/// </summary>
public record class CallStatement : ExecutableStatement
{
    protected CallStatement(WorkspaceUri parentUri, IExecutable target, ValuedExpression[] args)
        : base(parentUri)
    {
        Target = target;
        Arguments = args;
    }

    public IExecutable Target { get; }

    public ValuedExpression[] Arguments { get; }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        VBTypedValue? result = default;
        try
        {
            if (Target is VBTypeMember target)
            {
                var scope = context.EnterScope(target);
                foreach (var arg in Arguments.Reverse())
                {
                    // execute args in reverse order; if they're side-effecting
                    // then 
                    if (arg.Execute(context, rethrow) is VBTypedValue argValue)
                    {
                        scope.SetTypedValue(arg, argValue);
                    }
                    else
                    {
                        // we shouldn't be here

                    }
                }
                result = scope.Execute(context, rethrow);
            }
        }
        catch (VBRuntimeErrorException exception)
        {
            context.AddDiagnostics(exception);
            if (rethrow)
            {
                throw;
            }
        }
        catch (VBCompileErrorException exception)
        {
            context.AddDiagnostics(exception);
            if (rethrow)
            {
                throw;
            }
        }

        return result;
    }
}
