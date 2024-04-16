namespace Rubberduck.InternalApi.Model.Workspace;

public record class ProjectTemplate
{
    public const string TemplateSourceFolderName = ".template";

    public static ProjectTemplate Default { get; } = new();

    public string Rubberduck { get; init; } = ProjectFile.RubberduckVersion;
    public string Name { get; init; } = "EmptyProject";

    public ProjectFile ProjectFile { get; init; } = new();
}

public record class FileTemplates
{
    public string Rubberduck { get; init; } = ProjectFile.RubberduckVersion;
    public FileTemplate[] Templates { get; init; } = [];
}

public record class FileTemplate
{
    public string Key { get; init; } = "StdModule";
    public string Categories { get; init; } = "VBA,VB6";
    public bool IsSourceFile => Key.Contains("Module");

    public string DefaultFileName { get; init; } = "file";
    public string FileExtension { get; init; } = "vba";

    public string Content { get; init; } = string.Empty;
}
