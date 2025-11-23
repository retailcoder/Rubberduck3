using Rubberduck.InternalApi.Extensions;

namespace Rubberduck.InternalApi.Execution.Values;

public record class PrecompilerConstantValue : VBIntegerValue
{
    public PrecompilerConstantValue(string name, WorkspaceUri parentUri, int value)
    {
        Name = name;
        ParentUri = parentUri;
        NumericValue = value;
    }

    public string Name { get; init; }
    public WorkspaceUri ParentUri { get; init; }

    public bool IsWorkspaceScope => ParentUri is not WorkspaceFileUri;
    public bool IsFileScope => ParentUri is WorkspaceFileUri;
}
