using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Execution;
using Rubberduck.InternalApi.Model.Declarations.Execution.Values;
using System.Collections.Generic;
using System.Linq;

namespace Rubberduck.InternalApi.Model.Declarations.Symbols;

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

public record class CloseStatement : ExecutableStatement
{
    public CloseStatement(WorkspaceUri parentUri, IEnumerable<ValuedExpression> fileNumbers, LineLabelSymbol? parentLabel = null)
        : base(parentUri, parentLabel)
    {
        FileNumbers = fileNumbers;
    }

    public IEnumerable<ValuedExpression> FileNumbers { get; init; } = [];

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        if (!FileNumbers.Any())
        {
            context.CloseFile();
        }
        else
        {
            foreach (var fileNumber in FileNumbers)
            {
                if (fileNumber.Execute(context, rethrow) is VBTypedValue handle)
                {
                    context.CloseFile(handle);
                }
            }
        }

        return default;
    }
}

public record class ResetStatement : ExecutableStatement
{
    public ResetStatement(WorkspaceUri parentUri, LineLabelSymbol? parentLabel = null)
        : base(parentUri, parentLabel)
    {
    }
}

public record class SeekStatement : FileStatement
{
    public SeekStatement(WorkspaceUri parentUri,
        ValuedExpression fileNumber,
        ValuedExpression position,
        LineLabelSymbol? parentLabel = default)
        : base(parentUri, fileNumber, parentLabel)
    {
        Position = position;
    }

    public ValuedExpression Position { get; init; }
}

public record class LockStatement : FileStatement
{
    public LockStatement(WorkspaceUri parentUri,
        ValuedExpression fileNumber,
        ValuedExpression? startRecord = default,
        ValuedExpression? endRecord = default,
        LineLabelSymbol? parentLabel = default)
        : base(parentUri, fileNumber, parentLabel)
    {
        StartRecordNumber = startRecord;
        EndRecordNumber = endRecord;
    }

    public ValuedExpression? StartRecordNumber { get; init; }
    public ValuedExpression? EndRecordNumber { get; init; }
}

public record class UnlockStatement : FileStatement
{
    public UnlockStatement(WorkspaceUri parentUri,
        ValuedExpression fileNumber,
        ValuedExpression? startRecord = default,
        ValuedExpression? endRecord = default,
        LineLabelSymbol? parentLabel = default)
        : base(parentUri, fileNumber, parentLabel)
    {
        StartRecordNumber = startRecord;
        EndRecordNumber = endRecord;
    }

    public ValuedExpression? StartRecordNumber { get; init; }
    public ValuedExpression? EndRecordNumber { get; init; }
}

public record class LineInputStatement : FileStatement
{
    public LineInputStatement(WorkspaceUri parentUri,
        ValuedExpression fileNumber,
        TypedSymbol variable,
        LineLabelSymbol? parentLabel = default)
        : base(parentUri, fileNumber, parentLabel)
    {
        Variable = variable;
    }

    public TypedSymbol Variable { get; init; }
}

public record class WidthStatement : FileStatement
{
    public WidthStatement(WorkspaceUri parentUri, ValuedExpression fileNumber, ValuedExpression lineWidth, LineLabelSymbol? parentLabel = null)
        : base(parentUri, fileNumber, parentLabel)
    {
        LineWidth = lineWidth;
    }

    public ValuedExpression LineWidth { get; init; }
}

public record class PrintStatement : FileStatement
{
    public PrintStatement(WorkspaceUri parentUri, ValuedExpression fileNumber, StringValuedExpression expression, LineLabelSymbol? parentLabel = null)
        : base(parentUri, fileNumber, parentLabel)
    {
        Expression = expression;
    }

    public StringValuedExpression Expression { get; init; }
}

public record class WriteStatement : FileStatement
{
    public WriteStatement(WorkspaceUri parentUri, ValuedExpression fileNumber, StringValuedExpression expression, LineLabelSymbol? parentLabel = null)
        : base(parentUri, fileNumber, parentLabel)
    {
        Expression = expression;
    }

    public StringValuedExpression Expression { get; init; }
}

public record class InputStatement : FileStatement
{
    public InputStatement(WorkspaceUri parentUri, ValuedExpression fileNumber, IEnumerable<TypedSymbol> variables, LineLabelSymbol? parentLabel = null)
        : base(parentUri, fileNumber, parentLabel)
    {
        Variables = variables;
    }

    public IEnumerable<TypedSymbol> Variables { get; init; }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        // base implementation requires the file handle
        base.ExecuteInternal(context, rethrow);

        foreach (var symbol in Variables)
        {
            context.SetSymbolValue(symbol, VBEmptyValue.Empty.AsVariant());
        }

        return default;
    }
}

public record class PutStatement : FileStatement
{
    public PutStatement(WorkspaceUri parentUri, ValuedExpression fileNumber,
        ValuedExpression dataExpression,
        ValuedExpression? recordNumber = default,
        LineLabelSymbol? parentLabel = default)
        : base(parentUri, fileNumber, parentLabel)
    {
        DataExpression = dataExpression;
        RecordNumber = recordNumber;
    }

    public ValuedExpression DataExpression { get; init; }
    public ValuedExpression? RecordNumber { get; init; }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        DataExpression.Execute(context, rethrow);
        RecordNumber?.Execute(context, rethrow);
        return default;
    }
}

public record class GetStatement : FileStatement
{
    public GetStatement(WorkspaceUri parentUri, ValuedExpression fileNumber,
        TypedSymbol variable,
        ValuedExpression? recordNumber = default,
        LineLabelSymbol? parentLabel = default)
        : base(parentUri, fileNumber, parentLabel)
    {
        Variable = variable;
        RecordNumber = recordNumber;
    }

    public TypedSymbol Variable { get; init; }
    public ValuedExpression? RecordNumber { get; init; }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        RecordNumber?.Execute(context, rethrow);
        context.SetSymbolValue(Variable, VBEmptyValue.Empty.AsVariant());
        return default;
    }
}
