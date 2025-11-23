using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols.Expressions;

namespace Rubberduck.InternalApi.Model.Symbols.Statements.FileIO;

public record class OpenStatement : ExecutableStatement
{
    public OpenStatement(WorkspaceUri parentUri, ValuedExpression path, ValuedExpression fileNumber,
        VbOpenFileMode? mode = default, VbFileAccess? access = default, VbFileLock? fileLock = default,
        ValuedExpression? recordLength = default, LineLabelSymbol? parentLabel = default) : base(parentUri, parentLabel)
    {
        PathName = path;
        FileNumber = fileNumber;

        FileMode = mode;
        FileAccess = access;
        FileLock = fileLock;

        RecordLength = recordLength;
    }

    public ValuedExpression PathName { get; init; }
    public ValuedExpression FileNumber { get; init; }

    public VbOpenFileMode? FileMode { get; init; }
    public VbFileAccess? FileAccess { get; init; }
    public VbFileLock? FileLock { get; init; }
    public ValuedExpression? RecordLength { get; init; }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        if (FileNumber.Execute(context, rethrow) is VBTypedValue handle)
        {
            context.OpenFile(handle);
        }

        return default;
    }
}
