using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using System;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace Rubberduck.InternalApi.Model.Workspace;

public record class File
{
    public static File FromWorkspaceUri(WorkspaceFileUri workspaceUri) => new() 
    {
        RelativeUri = workspaceUri.RelativeUriString!
    };

    /// <summary>
    /// Gets the name of the file, without its extension.
    /// </summary>
    [JsonIgnore]
    public string Name => Path.GetFileNameWithoutExtension(RelativeUri.Split('/').Last());
    /// <summary>
    /// Gets the file's extension.
    /// </summary>
    [JsonIgnore]
    public string Extension => RelativeUri[^RelativeUri.LastIndexOf('.')..];

    /// <summary>
    /// The location of the module in the workspace, relative to the source root.
    /// </summary>
    /// <remarks>
    /// Value should come from a <c>WorkspaceUri</c> instance.
    /// </remarks>
    public string RelativeUri { get; set; }
    /// <summary>
    /// <c>true</c> if the module should open when the workspace is loaded in the Rubberduck Editor.
    /// </summary>
    public bool IsAutoOpen { get; set; }

    /// <summary>
    /// <c>true</c> if the <c>Uri</c> has a file extension that is handled by the specified language.
    /// </summary>
    public bool HasSourceFileExtension(SupportedLanguage language) => language.FileTypes.Any(e => RelativeUri.EndsWith(e[1..]));

    /// <summary>
    /// Gets a strongly-typed relative <c>WorkspaceUri</c> for the specified absolute workspace root.
    /// </summary>
    public WorkspaceFileUri GetWorkspaceUri(Uri workspaceRoot) => new WorkspaceFileUri(RelativeUri, workspaceRoot);

    [JsonIgnore]
    public bool IsLoadError { get; set; }
}
