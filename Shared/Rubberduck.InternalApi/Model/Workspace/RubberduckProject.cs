using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace Rubberduck.InternalApi.Model.Workspace;

/// <summary>
/// Determines the VB dialect and editor tooling available.
/// </summary>
public enum ProjectType
{
    /// <summary>
    /// RD3 project synchronizes with a host document.
    /// </summary>
    VBA,
    /// <summary>
    /// RD3 project synchronizes a .vbp project file.
    /// </summary>
    VB6,
}

public record class RubberduckProject
{
    /// <summary>
    /// The name of the project.
    /// </summary>
    public string Name { get; set; } = "Project1";
    /// <summary>
    /// The type of project.
    /// </summary>
    /// <remarks>
    /// Determines the VB dialect and editor tooling available.
    /// </remarks>
    public ProjectType ProjectType { get; set; }
    /// <summary>
    /// Project references.
    /// </summary>
    public Reference[] References { get; set; } = [Reference.VBStandardLibrary];
    /// <summary>
    /// Project source files that synchronize with a host VBA project.
    /// </summary>
    public Module[] Modules { get; set; } = Array.Empty<Module>();
    /// <summary>
    /// Any other files in the workspace, whether for code or non-code content.
    /// </summary>
    /// <remarks>
    /// For example a <c>README.md</c> or <c>LICENSE.md</c> markdown file could be part of the project (/repository) without synchronizing with the VBE.
    /// </remarks>
    public File[] OtherFiles { get; set; } = [];
    /// <summary>
    /// A relative URI for all folders in the project, whether they contain files or not.
    /// </summary>
    public string[] Folders { get; set; } = [];

    /// <summary>
    /// Gets all files in the project, regardless of whether they're source files or not.
    /// </summary>
    [JsonIgnore]
    public File[] AllFiles => Modules.Concat(OtherFiles).ToArray();
}
