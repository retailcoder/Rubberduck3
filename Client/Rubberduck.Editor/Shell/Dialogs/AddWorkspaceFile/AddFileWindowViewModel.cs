using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using Rubberduck.Resources;
using Rubberduck.UI.Services;
using Rubberduck.UI.Services.NewProject;
using Rubberduck.UI.Shared.NewProject;
using Rubberduck.UI.Shell;
using Rubberduck.UI.Shell.AddWorkspaceFile;
using Rubberduck.UI.Windows;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Abstractions;
using System.Linq;
using System.Windows.Data;
using Resx = Rubberduck.Resources.v3.DialogsUI;

namespace Rubberduck.Editor.Shell.Dialogs.AddWorkspaceFile;

public class AddFileWindowViewModel : DialogWindowViewModel, IAddFileWindowViewModel
{
    private readonly IFileSystem _fileSystem;

    public AddFileWindowViewModel(UIServiceHelper service, IWindowChromeViewModel chrome)
        : base(service, Resx.ResourceManager.GetLocalizedString("AddFile_Title"), [], chrome)
    {
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
    public WorkspaceFileUri WorkspaceFileUri => new(_fileSystem.Path.Combine(ParentFolderUri.RelativeUriString!, $"{Name}{SelectedTemplate?.FileExtension}"), ParentFolderUri.WorkspaceRoot);

    private ICollectionView _templatesView;
    public ICollectionView TemplatesView
    {
        get => _templatesView;
        set
        {
            if (_templatesView != value)
            {
                _templatesView = value;
                OnPropertyChanged();
            }
        }
    }

    private IEnumerable<IFileTemplate> _templates;
    public IEnumerable<IFileTemplate> Templates
    {
        get => _templates;
        set
        {
            if (_templates != value)
            {
                _templates = value;
                OnPropertyChanged();

                TemplatesView = CollectionViewSource.GetDefaultView(value);
                TemplatesView.Filter = (item) => string.IsNullOrWhiteSpace(_selection) || item is IFileTemplate e && e.Categories.Contains(_selection);
            }
        }
    }

    private IFileTemplate _selectedTemplate;
    public IFileTemplate? SelectedTemplate
    {
        get => _selectedTemplate;
        set
        {
            if (_selectedTemplate != value && value is not null)
            {
                _selectedTemplate = value;
                OnPropertyChanged();

                if (string.IsNullOrWhiteSpace(_name))
                {
                    Name = value.DefaultFileName;
                }
            }
        }
    }
    public IEnumerable<string> Categories { get; init; } = [SupportedLanguage.VBA.Id, SupportedLanguage.VB6.Id, "Other"];
    private string _selection;
    public string Selection
    {
        get => _selection;
        set
        {
            if (value != _selection)
            {
                _selection = value;
                OnPropertyChanged();
                TemplatesView.Refresh();
            }
        }
    }
}
