using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using System;
using System.Linq;

namespace Rubberduck.InternalApi.Model.Workspace;

public record class File
{
    public static File FromWorkspaceUri(WorkspaceFileUri workspaceUri) => new() 
    {
        Name = workspaceUri.FileNameWithoutExtension,
        Uri = workspaceUri.RelativeUriString!
    };

    /// <summary>
    /// The name of the file; must be unique across the entire workspace.
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// The location of the module in the workspace, relative to the source root.
    /// </summary>
    /// <remarks>
    /// Value should come from a <c>WorkspaceUri</c> instance.
    /// </remarks>
    public string Uri { get; set; }
    /// <summary>
    /// <c>true</c> if the module should open when the workspace is loaded in the Rubberduck Editor.
    /// </summary>
    public bool IsAutoOpen { get; set; }

    /// <summary>
    /// <c>true</c> if the <c>Uri</c> has a file extension that is handled by the specified language.
    /// </summary>
    public bool HasSourceFileExtension(SupportedLanguage language) => language.FileTypes.Any(e => Uri.EndsWith(e[1..]));

    /// <summary>
    /// Gets a strongly-typed relative <c>WorkspaceUri</c> for the specified absolute workspace root.
    /// </summary>
    public WorkspaceFileUri GetWorkspaceUri(Uri workspaceRoot) => new WorkspaceFileUri(Uri, workspaceRoot);
}
