using System.Text.Json.Serialization;

namespace Rubberduck.InternalApi.Model.Workspace;

public record class ProjectTemplate
{
    public const string TemplateSourceFolderName = ".template";

    public static ProjectTemplate Default { get; } = new();

    public string Rubberduck { get; init; } = ProjectFile.RubberduckVersion.ToString(2);
    public string Name { get; init; } = "EmptyProject";

    public ProjectFile ProjectFile { get; init; } = new();
}

public record class FileTemplates
{
    public string Rubberduck { get; init; } = ProjectFile.RubberduckVersion.ToString(2);
    public FileTemplate[] FileTypes { get; init; } = [];
}

public record class FileTemplate
{
    public static readonly string NameContentMarker = "{%NAME%}";

    public string Name { get; init; }
    public string Description { get; init; }
    public string[] Categories { get; init; }
    public string DefaultFileName { get; init; }
    public string FileExtension { get; init; }
    public string ContentUri { get; init; }

    [JsonIgnore]
    public string TextContent { get; init; }
}
