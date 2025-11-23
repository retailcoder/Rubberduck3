using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;

namespace Rubberduck.InternalApi.Model.Symbols.Statements;

/// <summary>
/// Represents an executable VBA statement.
/// </summary>
public abstract record class ExecutableStatement : IExecutable
{
    protected ExecutableStatement(WorkspaceUri parentUri, LineLabelSymbol? parentLabel = default)
    {
        ParentUri = parentUri;
        ParentLabel = parentLabel;
    }

    /// <summary>
    /// The URI of the parent symbol.
    /// </summary>
    /// <remarks>
    /// This information should not be used to construct a symbol hierarchy.
    /// </remarks>
    public WorkspaceUri ParentUri { get; init; }

    /// <summary>
    /// The line label (or number) associated with this statement, if any.
    /// </summary>
    public LineLabelSymbol? ParentLabel { get; init; }

    /// <summary>
    /// Executes the statement in the given context.
    /// </summary>
    public VBTypedValue? Execute(VBExecutionContext context, bool rethrow = false) => ExecuteInternal(context, rethrow);

    protected virtual VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false) => default;
}
