using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols.Expressions;

namespace Rubberduck.InternalApi.Model.Symbols.Statements.FileIO;

public enum VbOpenFileMode
{
    Append,
    Binary,
    Input,
    Output,
    Random
}

public enum VbFileAccess
{
    Read,
    Write,
    ReadWrite
}

public enum VbFileLock
{
    Shared,
    LockRead,
    LockWrite,
    LockReadWrite
}

public abstract record class FileStatement : ExecutableStatement
{
    public FileStatement(WorkspaceUri parentUri, ValuedExpression fileNumber, LineLabelSymbol? parentLabel = null)
        : base(parentUri, parentLabel)
    {
        FileNumber = fileNumber;
    }

    public ValuedExpression FileNumber { get; init; }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        if (FileNumber.Execute(context, rethrow) is VBTypedValue handle)
        {
            context.RequireFileHandle(handle);
        }

        return default;
    }
}
