using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Workspace;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using Rubberduck.Resources;
using Rubberduck.UI;
using Rubberduck.UI.AddFile;
using Rubberduck.UI.Chrome;
using Rubberduck.UI.Services;
using Rubberduck.UI.Services.NewProject;
using Rubberduck.UI.Windows;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using Resx = Rubberduck.Resources.v3.DialogsUI;

namespace Rubberduck.Editor.Shell.Dialogs;

public class FileTemplateViewModel : IFileTemplate
{
    private readonly string _template;

    public FileTemplateViewModel(FileTemplate model)
    {
        Key = model.Key;
        FileExtension = model.FileExtension;
        DefaultFileName = model.DefaultFileName;
        Categories = model.Categories.Split(',');
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

public class AddFileWindowViewModel : DialogWindowViewModel, IAddFileWindowViewModel
{
    private readonly IFileSystem _fileSystem;

    public AddFileWindowViewModel(UIServiceHelper service, IWindowChromeViewModel chrome)
        : base(service, Resx.ResourceManager.GetLocalizedString("AddFile_Title"), [], chrome)
    {
    }

    protected override void ResetToDefaults()
    {
        throw new System.NotImplementedException();
    }

    private string _name;
    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
            }
        }
    }

    public WorkspaceFolderUri ParentFolderUri { get; init; }
    public WorkspaceFileUri WorkspaceFileUri => new(_fileSystem.Path.Combine(ParentFolderUri.RelativeUriString!, $"{Name}{SelectedFileTemplate?.FileExtension}"), ParentFolderUri.WorkspaceRoot);

    public IEnumerable<IFileTemplate> Templates { get; init; } = [];

    private IFileTemplate _selectedTemplate;
    public IFileTemplate? SelectedTemplate
    {
        get => _selectedTemplate;
        set
        {
            if (_selectedTemplate != value)
            {
                _selectedTemplate = value;
                OnPropertyChanged();
            }
        }
    }
    public IEnumerable<string> Categories { get; init; } = [SupportedLanguage.VBA.Id, SupportedLanguage.VB6.Id];

}
