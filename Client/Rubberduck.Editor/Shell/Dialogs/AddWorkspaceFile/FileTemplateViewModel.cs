using Rubberduck.InternalApi.Model.Workspace;
using Rubberduck.UI.Shell.AddWorkspaceFile;
using System.Collections.Generic;

namespace Rubberduck.Editor.Shell.Dialogs.AddWorkspaceFile;

public class FileTemplateViewModel : IFileTemplate
{
    public FileTemplateViewModel(FileTemplate model)
    {
        Name = model.Name;
        Description = model.Description;
        Categories = model.Categories;
        DefaultFileName = model.DefaultFileName;
        FileExtension = model.FileExtension;
        ContentUri = model.ContentUri;
    }

    public string FileExtension { get; }
    public string DefaultFileName { get; }

    public bool IsSourceFile { get; }

    public IEnumerable<string> Categories { get; }
    public string Name { get; }
    public string Description{ get; }

    public string ContentUri { get; }
}
