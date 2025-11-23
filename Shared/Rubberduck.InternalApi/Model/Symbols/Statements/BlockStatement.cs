using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using System.Collections.Generic;

namespace Rubberduck.InternalApi.Model.Symbols.Statements;

/// <summary>
/// Represents a block statement, that can contain child executable statements.
/// </summary>
public abstract record class BlockStatement : ExecutableStatement
{
    public BlockStatement(WorkspaceUri parentUri, IEnumerable<IExecutable>? children = null, LineLabelSymbol? parentLabel = null)
        : base(parentUri, parentLabel)
    {
        Children = children ?? [];
    }

    public IEnumerable<IExecutable> Children { get; }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        var success = ExecuteBody(context, rethrow);
        return new VBBooleanValue().WithValue(success);
    }

    protected bool ExecuteBody(VBExecutionContext context, bool rethrow = false)
    {
        try
        {
            context.CurrentScope.EnterBlock();
            foreach (var child in Children)
            {
                _ = child.Execute(context, rethrow);
            }
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (!context.CurrentScope.TryExitBlock(this))
            {
                // we shouldn't be here
            }
        }
    }
}
