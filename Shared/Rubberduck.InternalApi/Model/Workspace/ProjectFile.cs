using System;
using System.Text.Json.Serialization;

namespace Rubberduck.InternalApi.Model.Workspace;

public record class ProjectFile
{
    /// <summary>
    /// The file name of a <em>Rubberduck project file</em>.
    /// </summary>
    public const string FileName = ".rdproj";
        
    // TODO find a better home for this
    public static readonly Version RubberduckVersion = new("3.0");

    /// <summary>
    /// The absolute workspace root location where the project file is.
    /// </summary>
    /// <remarks>This property is not serialized.</remarks>
    [JsonIgnore]
    public Uri Uri { get; init; } = default!;

    /// <summary>
    /// The Rubberduck version that created the file.
    /// </summary>
    public string Rubberduck { get; init; } = RubberduckVersion.ToString(2);

    /// <summary>
    /// The <c>Version</c> that corresponds to the <c>Rubberduck</c> version string.
    /// </summary>
    [JsonIgnore]
    public Version Version => new(Rubberduck);

    /// <summary>
    /// <c>true</c> if the <c>Version</c> of this <c>ProjectFile</c> is less than or equal to the <c>RubberduckVersion</c>.
    /// </summary>
    /// <remarks>
    /// Let's presume 3.x backward compatibility up to v3.0.
    /// </remarks>
    public bool ValidateVersion() => Version <= RubberduckVersion && Version.Major >= 3;

    /// <summary>
    /// Information about the VBA project.
    /// </summary>
    public RubberduckProject VBProject { get; init; } = new();

    public ProjectFile WithUri(Uri uri) => this with { Uri = uri };

    public override int GetHashCode() => Uri.GetHashCode();
}
