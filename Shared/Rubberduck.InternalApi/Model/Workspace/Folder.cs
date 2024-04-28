using Rubberduck.InternalApi.Extensions;
using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace Rubberduck.InternalApi.Model.Workspace;

public record class Folder
{
    public static Folder FromWorkspaceUri(WorkspaceFolderUri workspaceUri) => new()
    {
        RelativeUri = workspaceUri.RelativeUriString!
    };

    /// <summary>
    /// The name of the folder.
    /// </summary>
    [JsonIgnore]
    public string Name => RelativeUri.Replace('/', '\\').Split('\\').Last();

    /// <summary>
    /// The location of the module in the workspace, relative to the source root.
    /// </summary>
    public string RelativeUri { get; set; } = string.Empty;

    /// <summary>
    /// Gets a strongly-typed relative <c>WorkspaceUri</c> for the specified absolute workspace root.
    /// </summary>
    public WorkspaceFolderUri GetWorkspaceUri(Uri workspaceRoot) => new WorkspaceFolderUri(RelativeUri, workspaceRoot);

    [JsonIgnore]
    public bool IsLoadError { get; set; }
}
