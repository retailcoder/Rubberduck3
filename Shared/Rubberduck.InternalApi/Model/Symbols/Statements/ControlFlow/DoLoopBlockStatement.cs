using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols.Expressions;
using Rubberduck.InternalApi.Model.Symbols.Statements;
using System.Collections.Generic;

namespace Rubberduck.InternalApi.Model.Symbols.Statements.ControlFlow;

public enum DoLoopType
{
    /// <summary>
    /// An unconditioned Do...Loop block
    /// </summary>
    DoLoop,
    /// <summary>
    /// Preconditional Do While...Loop block
    /// </summary>
    DoWhile,
    /// <summary>
    /// Preconditional Do Until...Loop block
    /// </summary>
    DoUntil,
    /// <summary>
    /// Postconditional Do... Loop While block
    /// </summary>
    LoopWhile,
    /// <summary>
    /// Postconditional Do... Loop Until block
    /// </summary>
    LoopUntil
}


public abstract record class DoLoopBlockStatement : BlockStatement
{
    protected DoLoopBlockStatement(WorkspaceUri parentUri, IEnumerable<IExecutable>? children = null, DoLoopType loopType = DoLoopType.DoLoop, BooleanValuedExpression? condition = null)
        : base(parentUri, children)
    {
        LoopType = loopType;
        if (loopType != DoLoopType.DoLoop)
        {
            Condition = condition;
        }
    }

    public DoLoopType LoopType { get; }
    public BooleanValuedExpression? Condition { get; }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        var didEnter = false;
        var scope = context.CurrentScope;
        switch (LoopType)
        {
            case DoLoopType.DoLoop:
                didEnter = true;
                ExecuteBody(context, rethrow);
                break; // unconditioned loop, execute body once

            case DoLoopType.DoUntil:
            case DoLoopType.DoWhile:
                while (Condition?.Execute(context, rethrow) is VBBooleanValue vbBool && !vbBool.Value)
                {
                    didEnter = true;
                    ExecuteBody(context, rethrow);
                    break; // only execute once for now
                }
                break;

            case DoLoopType.LoopUntil:
            case DoLoopType.LoopWhile:
                do
                {
                    didEnter = true;
                    ExecuteBody(context, rethrow);
                    break; // only execute once for now
                }
                while (Condition?.Execute(context, rethrow) is VBBooleanValue vbBool && vbBool.Value);
                break;

            default:
                break;
        }

        return new VBBooleanValue() { Value = didEnter };
    }
}

/// <summary>
/// Represents an unconditioned Do...Loop executable block.
/// </summary>
public sealed record class DoLoopStatement : DoLoopBlockStatement
{
    public DoLoopStatement(WorkspaceUri parentUri, IEnumerable<IExecutable>? children = null)
        : base(parentUri, children, DoLoopType.DoLoop)
    {
    }
}

/// <summary>
/// Represents a preconditional Do... Loop While executable block.
/// </summary>
public sealed record class DoWhileLoopStatement : DoLoopBlockStatement
{
    public DoWhileLoopStatement(WorkspaceUri parentUri, BooleanValuedExpression condition, IEnumerable<IExecutable>? children = null)
        : base(parentUri, children, DoLoopType.DoWhile, condition)
    {
    }
}

/// <summary>
/// Represents a preconditional Do... Loop Until executable block.
/// </summary>
public sealed record class DoUntilLoopStatement : DoLoopBlockStatement
{
    public DoUntilLoopStatement(WorkspaceUri parentUri, BooleanValuedExpression condition, IEnumerable<IExecutable>? children = null)
        : base(parentUri, children, DoLoopType.DoUntil, condition)
    {
    }
}

/// <summary>
/// Represents a postconditional Do... Loop While executable block.
/// </summary>
public sealed record class LoopWhileDoStatement : DoLoopBlockStatement
{
    public LoopWhileDoStatement(WorkspaceUri parentUri, BooleanValuedExpression condition, IEnumerable<IExecutable>? children = null)
        : base(parentUri, children, DoLoopType.LoopWhile, condition)
    {
    }
}

/// <summary>
/// Represents a postconditional Do... Loop Until executable block.
/// </summary>
public sealed record class LoopUntilDoStatement : DoLoopBlockStatement
{
    public LoopUntilDoStatement(WorkspaceUri parentUri, BooleanValuedExpression condition, IEnumerable<IExecutable>? children = null)
        : base(parentUri, children, DoLoopType.LoopUntil, condition)
    {
    }
}
