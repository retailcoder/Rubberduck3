using Rubberduck.InternalApi.Extensions;
using System;
using System.Text.Json.Serialization;

namespace Rubberduck.InternalApi.Model.Workspace;

public record class Folder
{
    public static Folder FromWorkspaceUri(WorkspaceFolderUri workspaceUri) => new()
    {
        Name = workspaceUri.FolderName,
        Uri = workspaceUri.RelativeUriString!
    };

    /// <summary>
    /// The name of the folder.
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// The location of the module in the workspace, relative to the source root.
    /// </summary>
    public string Uri { get; set; }

    /// <summary>
    /// Gets a strongly-typed relative <c>WorkspaceUri</c> for the specified absolute workspace root.
    /// </summary>
    public WorkspaceFolderUri GetWorkspaceUri(Uri workspaceRoot) => new WorkspaceFolderUri(Uri, workspaceRoot);

    [JsonIgnore]
    public bool IsLoadError { get; set; }
}
