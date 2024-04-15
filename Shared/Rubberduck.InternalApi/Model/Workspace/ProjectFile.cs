﻿using System;
using System.Text.Json.Serialization;

namespace Rubberduck.InternalApi.Model.Workspace;

public record class ProjectFile
{
    /// <summary>
    /// The file name of a <em>Rubberduck project file</em>.
    /// </summary>
    public const string FileName = ".rdproj";
        
    // TODO find a better home for this
    public static readonly string RubberduckVersion = "3.0";

    /// <summary>
    /// The absolute workspace root location where the project file is.
    /// </summary>
    /// <remarks>This property is not serialized.</remarks>
    [JsonIgnore]
    public Uri Uri { get; init; } = default!;

    /// <summary>
    /// The Rubberduck version that created the file.
    /// </summary>
    public string Rubberduck { get; init; } = RubberduckVersion;

    /// <summary>
    /// Information about the VBA project.
    /// </summary>
    public RubberduckProject VBProject { get; init; } = new();

    public ProjectFile WithUri(Uri uri) => this with { Uri = uri };
}
