using Rubberduck.InternalApi.Model.Workspace;
using Rubberduck.Resources;
using Rubberduck.UI.Shell.AddWorkspaceFile;
using System.Collections.Generic;
using Resx = Rubberduck.Resources.v3.DialogsUI;

namespace Rubberduck.Editor.Shell.Dialogs.AddWorkspaceFile;

public class FileTemplateViewModel : IFileTemplate
{
    private readonly string _template;

    public FileTemplateViewModel(FileTemplate model)
    {
        Key = model.Key;
        FileExtension = model.FileExtension;
        DefaultFileName = model.DefaultFileName;
        Categories = model.Categories;
        _template = model.Content;
    }

    public string GetTextContent(string filename) => _template.Replace("%NAME%", filename);

    public string FileExtension { get; }
    public string DefaultFileName { get; }

    public bool IsSourceFile { get; }

    public string Key { get; }
    public IEnumerable<string> Categories { get; }
    public string Name => Resx.ResourceManager.GetLocalizedString($"FileTemplate_{Key}_Name");
    public string Description => Resx.ResourceManager.GetLocalizedString($"FileTemplate_{Key}_Description");
}
