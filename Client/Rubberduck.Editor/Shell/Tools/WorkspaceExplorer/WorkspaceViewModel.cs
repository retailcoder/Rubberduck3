using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Workspace;
using Rubberduck.InternalApi.Services.IO.Abstract;
using Rubberduck.UI;
using Rubberduck.UI.Command.StaticRouted;
using Rubberduck.UI.Shell.Tools.WorkspaceExplorer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;

namespace Rubberduck.Editor.Shell.Tools.WorkspaceExplorer;

public class WorkspaceViewModel : ViewModelBase, IWorkspaceTreeNode, IWorkspaceViewModel
{
    public event EventHandler<CancelEventArgs> NameEditCompleted = delegate { };

    public static WorkspaceViewModel FromModel(ProjectFile model, IWorkspaceIOServices ioServices)
    {
        var vm = new WorkspaceViewModel(model, ioServices)
        {
            Name = model.VBProject.Name,
            DisplayName = model.VBProject.Name,
            Uri = new WorkspaceFolderUri(null, new Uri(ioServices.Path.Combine(model.Uri.LocalPath, WorkspaceUri.SourceRootName + "\\"))),
            //IsFileSystemWatcherEnabled = service.IsFileSystemWatcherEnabled(model.Uri),
            IsExpanded = true
        };

        var sourceFiles = model.VBProject.Modules.Select(e => WorkspaceSourceFileViewModel.FromModel(e, model.Uri) as WorkspaceTreeNodeViewModel);
        var otherFiles = model.VBProject.OtherFiles.Select(e => WorkspaceFileViewModel.FromModel(e, model.Uri) as WorkspaceTreeNodeViewModel);
        var projectFolders = model.VBProject.Folders.Select(relativeUri => WorkspaceFolderViewModel.FromModel(new Folder { RelativeUri = relativeUri }, model.Uri)).OrderBy(e => e.Name).ToList();
        var rootFolders = projectFolders.Where(e => ((WorkspaceFolderUri)e.Uri).RelativeUriString!.Count(c => c == '/') == 0);

        var projectFilesByFolder = sourceFiles.Concat(otherFiles)
            .Select(e => (Folder:((WorkspaceFileUri)e.Uri).WorkspaceFolder, File:e))
            .GroupBy(e => string.IsNullOrWhiteSpace(e.Folder.RelativeUriString) ? WorkspaceUri.SourceRootName : e.Folder.RelativeUriString)
            .ToDictionary(group => Path.TrimEndingDirectorySeparator(group.Key), group => group.Select(e => e.File).ToArray().AsEnumerable());

        foreach (var folder in rootFolders)
        {
            AddFolderContent(ioServices, folder, projectFilesByFolder, projectFolders);
            vm.AddChildNode(folder);
        }

        var srcRoot = new WorkspaceFolderUri(null, model.Uri);
        if (projectFilesByFolder.TryGetValue(srcRoot.AbsoluteLocation.LocalPath, out var rootFolderFiles))
        {
            var rootFilePaths = rootFolderFiles.OrderBy(e => e.Name).Select(e => e.Uri.LocalPath).ToHashSet();
            AddFolderFileNodes(ioServices, vm, rootFolderFiles, rootFilePaths);

            projectFilesByFolder.Remove(srcRoot.AbsoluteLocation.LocalPath);
        }

        foreach (var key in projectFilesByFolder.Keys)
        {
            var folder = CreateWorkspaceFolderNode(ioServices, projectFilesByFolder[key], new WorkspaceFolderUri(key, model.Uri));
            vm.AddChildNode(folder);
        }

        return vm;
    }

    private static void AddFolderContent(IWorkspaceIOServices ioServices, WorkspaceFolderViewModel folder, IDictionary<string, IEnumerable<WorkspaceTreeNodeViewModel>> projectFilesByFolder, IEnumerable<WorkspaceFolderViewModel> projectFolders)
    {
        var workspaceFolderUri = (WorkspaceFolderUri)folder.Uri;
        var key = Path.TrimEndingDirectorySeparator(workspaceFolderUri.RelativeUriString ?? WorkspaceUri.SourceRootName);

        var localPath = ((WorkspaceFolderUri)folder.Uri).AbsoluteLocation.LocalPath;
        foreach (var subFolder in projectFolders.Where(e => localPath == new System.IO.DirectoryInfo(new WorkspaceFolderUri(e.Uri.OriginalString, workspaceFolderUri.WorkspaceRoot).AbsoluteLocation.LocalPath).Parent!.FullName))
        {
            AddFolderContent(ioServices, subFolder, projectFilesByFolder, projectFolders);
            folder.AddChildNode(subFolder);
        }

        var projectFolderPaths = projectFolders.Select(e => e.Uri.AbsoluteLocation.LocalPath).ToHashSet();

        try
        {
            foreach (var subFolder in Directory.EnumerateDirectories(localPath))
            {
                if (!projectFolderPaths.Contains(subFolder))
                {
                    var ghostUri = new WorkspaceFolderUri(subFolder, workspaceFolderUri.WorkspaceRoot);
                    var node = new WorkspaceFolderViewModel { IsInProject = false, Uri = ghostUri, Name = ghostUri.FolderName };
                    AddFolderContent(ioServices, node, projectFilesByFolder, projectFolders);

                    folder.AddChildNode(node);
                    AddFolderFileNodes(ioServices, node, [], []);
                }
            }
        }
        catch (Exception)
        {
            folder.IsLoadError = true;
        }

        // add the files last to keep folders together
        if (projectFilesByFolder.TryGetValue(key, out var projectFiles) && projectFiles is not null)
        {
            var projectFilePaths = projectFiles.Select(file => ((WorkspaceFileUri)file.Uri).AbsoluteLocation.LocalPath).ToHashSet();
            AddFolderFileNodes(ioServices, folder, projectFiles, projectFilePaths);

            projectFilesByFolder.Remove(key);
        }
    }

    private static WorkspaceFolderViewModel CreateWorkspaceFolderNode(IWorkspaceIOServices ioServices, IEnumerable<IWorkspaceTreeNode> projectFiles, WorkspaceFolderUri uri)
    {
        var folder = new WorkspaceFolderViewModel
        {
            IsInProject = true,
            Uri = uri,
            Name = uri.FolderName
        };

        var projectFilePaths = projectFiles.Select(file => file.Uri.AbsoluteLocation.LocalPath).ToHashSet();
        AddFolderFileNodes(ioServices, folder, projectFiles, projectFilePaths);
        return folder;
    }

    private static void AddFolderFileNodes(IWorkspaceIOServices ioServices, IWorkspaceTreeNode folder, IEnumerable<IWorkspaceTreeNode> projectFiles, HashSet<string> projectFilePaths)
    {
        var workspaceFiles = GetWorkspaceFilesNotInProject(ioServices, folder, projectFilePaths);
        foreach (var file in projectFiles.Concat(workspaceFiles))
        {
            folder.AddChildNode(file);
        }
    }

    private static IEnumerable<WorkspaceFileViewModel> GetWorkspaceFilesNotInProject(IWorkspaceIOServices ioServices, IWorkspaceTreeNode folder, HashSet<string> projectFilePaths)
    {
        var workspaceRoot = ((WorkspaceFolderUri)folder.Uri).WorkspaceRoot;
        var allFiles = ioServices.WorkspaceFolder.GetFiles((WorkspaceFolderUri)folder.Uri);
        
        var results = allFiles.Except(projectFilePaths).Select(file => new WorkspaceFileViewModel
                    {
                        Uri = new WorkspaceFileUri(file.Replace($"\\{WorkspaceUri.SourceRootName}", string.Empty)[workspaceRoot.LocalPath.Length..], workspaceRoot),
                        Name = ioServices.Path.GetFileNameWithoutExtension(file),
                        IsInProject = false // file exists in a project folder but is not included in the project
                    });
        return results;
    }

    public ICollectionView ItemsViewSource { get; }

    public virtual IEnumerable<object> ContextMenuItems => new object[]
            {
                new MenuItem { Command = WorkspaceExplorerCommands.CreateFileCommand, CommandParameter = Uri },
                new MenuItem { Command = WorkspaceExplorerCommands.CreateFolderCommand, CommandParameter = Uri },
                new Separator(),
                new MenuItem { Command = FileCommands.OpenFolderInWindowsExplorerCommand, CommandParameter = Uri },
                new MenuItem { Command = FileCommands.RenameProjectWorkspaceCommand, CommandParameter = Uri },
                new Separator(),
                new MenuItem { Command = FileCommands.SynchronizeProjectWorkspaceCommand, CommandParameter = Uri },
                new Separator(),
                new MenuItem { Command = FileCommands.CloseProjectWorkspaceCommand, CommandParameter = Uri },
            };

    private readonly IWorkspaceIOServices _ioServices;

    public WorkspaceViewModel(ProjectFile model, IWorkspaceIOServices ioServices)
    {
        _ioServices = ioServices;

        ItemsViewSource = CollectionViewSource.GetDefaultView(_children);
        ItemsViewSource.Filter = o => ((IWorkspaceTreeNode)o).IsVisible && (ShowAllFiles || ((IWorkspaceTreeNode)o).IsInProject);

        Model = model;
        IsInProject = true;
    }

    public ProjectFile Model { get; init; }

    private bool _isFileSystemWatcherEnabled;
    public bool IsFileSystemWatcherEnabled
    {
        get => _isFileSystemWatcherEnabled;
        set
        {
            if (_isFileSystemWatcherEnabled != value)
            {
                _isFileSystemWatcherEnabled = value;
                // TODO affect actual watcher
                OnPropertyChanged();
            }
        }
    }

    public void IncludeInProject(WorkspaceUri uri)
    {
        var result = FindNode(uri, this);
        if (result != null)
        {
            var (node, parent) = result.Value;
            node.IsInProject = true;

            if (node is WorkspaceSourceFileViewModel sourceFile)
            {
                Model.VBProject.Modules = Model.VBProject.Modules.Append(sourceFile.Module).ToArray();
            }
            else if (node is WorkspaceFileViewModel file)
            {
                Model.VBProject.OtherFiles = Model.VBProject.OtherFiles.Append(file.Model).ToArray();
            }
            else if (node is WorkspaceFolderViewModel folder)
            {
                Model.VBProject.Folders = Model.VBProject.Folders.Append(folder.Uri.RelativeUriString!).ToHashSet().ToArray();
            }
        }
    }

    private (IWorkspaceTreeNode, IWorkspaceTreeNode)? FindNode(WorkspaceUri uri, IWorkspaceTreeNode root)
    {
        var target = uri.AbsoluteLocation.LocalPath;
        if (root.Uri.AbsoluteLocation.LocalPath == target)
        {
            return (root, root);
        }

        foreach (var node in root.Children)
        {
            if (node.Uri.AbsoluteLocation.LocalPath == target)
            {
                return (node, root);
            }

            if (node.Children.Count > 0)
            {
                foreach (var child in node.Children)
                {
                    var found = FindNode(uri, child);
                    if (found != default)
                    {
                        return found;
                    }
                }
            }
        }

        return default;
    }

    public void ExcludeFromProject(WorkspaceUri uri)
    {
        var result = FindNode(uri, this);
        if (result != null)
        {
            var (node, parent) = result.Value;
            node.IsInProject = false;
            node.IsVisible = !node.IsLoadError;

            parent.Children.Remove(node);
            ItemsViewSource.Refresh();

            if (node is WorkspaceSourceFileViewModel sourceFile)
            {
                Model.VBProject.Modules = Model.VBProject.Modules.Where(e => e.RelativeUri != uri.RelativeUriString).ToArray();
            }
            else if (node is WorkspaceFileViewModel file)
            {
                Model.VBProject.OtherFiles = Model.VBProject.OtherFiles.Where(e => e.RelativeUri != uri.RelativeUriString).ToArray();
            }
            else if (node is WorkspaceFolderViewModel folder)
            {
                Model.VBProject.Folders = Model.VBProject.Folders.Where(e => !e.StartsWith(folder.Uri.RelativeUriString!)).ToArray();
                Model.VBProject.Modules = Model.VBProject.Modules.Where(e => !e.RelativeUri.StartsWith(uri.RelativeUriString!)).ToArray();
                Model.VBProject.OtherFiles = Model.VBProject.OtherFiles.Where(e => !e.RelativeUri.StartsWith(uri.RelativeUriString!)).ToArray();
            }
        }
    }

    private readonly ObservableCollection<IWorkspaceTreeNode> _children = new();
    public ObservableCollection<IWorkspaceTreeNode> Children => _children;

    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; }

    public bool IsEditingName { get; set; }
    private string _editName;
    public virtual string EditName
    {
        get => _editName;
        set
        {
            if (_editName != value)
            {
                var oldName = Name;

                _editName = value;
                OnPropertyChanged();

                var args = new CancelEventArgs();
                NameEditCompleted?.Invoke(this, args);

                if (args.Cancel)
                {
                    _editName = oldName;
                    OnPropertyChanged();
                }
                IsEditingName = false;
            }
        }
    }

    private bool _showFileExtensions;
    public bool ShowFileExtensions
    {
        get => _showFileExtensions;
        set
        {
            if (_showFileExtensions != value)
            {
                _showFileExtensions = value;
                foreach (var node in Children)
                {
                    node.ShowFileExtensions = _showFileExtensions;
                }
                OnPropertyChanged();
                ItemsViewSource.Refresh();
            }
        }
    }

    private bool _showAllFiles;
    public bool ShowAllFiles
    {
        get => _showAllFiles;
        set
        {
            if (_showAllFiles != value)
            {
                _showAllFiles = value;
                foreach (var node in Children)
                {
                    node.ShowAllFiles = _showAllFiles;
                }
                OnPropertyChanged();
                ItemsViewSource.Refresh();
            }
        }
    }

    public WorkspaceUri Uri { get; set; } = default!; // FIXME this will come back to bite me...
    public string FileName { get; set; } = ProjectFile.FileName;

    public void AddChildNode(IWorkspaceTreeNode childNode)
    {
        _children.Add(childNode);
    }

    public IWorkspaceTreeNode? FindChildNode(WorkspaceUri uri, IWorkspaceTreeNode? parent = null)
    {
        if (parent is null)
        {
            parent = this;
        }

        foreach (var child in parent.Children)
        {
            if (child.Uri == uri)
            {
                return child;
            }

            var recursive = FindChildNode(uri, child);
            if (recursive != null)
            {
                return recursive;
            }
        }

        return null;
    }

    public void RemoveWorkspaceUri(WorkspaceFileUri uri)
    {
        var node = FindChildNode(uri);
        if (node != null)
        {
            node.IsLoadError = true;
            node.IsVisible = false;
            ItemsViewSource.Refresh();
        }
    }

    public void RemoveWorkspaceUri(WorkspaceFolderUri uri)
    {
        var node = FindChildNode(uri);
        if (node != null)
        {
            node.IsLoadError = true;
            node.IsVisible = false;
            ItemsViewSource.Refresh();
        }
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isFiltered;
    public bool Filtered
    {
        get => _isFiltered;
        set
        {
            if (_isFiltered != value)
            {
                _isFiltered = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded != value)
            {
                _isExpanded = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isInProject;
    public bool IsInProject
    {
        get => _isInProject;
        set
        {
            if (_isInProject != value)
            {
                _isInProject = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isLoadError;
    public bool IsLoadError
    {
        get => _isLoadError;
        set
        {
            if (_isLoadError != value)
            {
                _isLoadError = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isVisible = true;
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible != value)
            {
                _isVisible = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isDeleted;
    public bool IsDeleted
    {
        get => _isDeleted;
        set
        {
            if (_isDeleted != value)
            {
                _isDeleted = value;
                OnPropertyChanged();
            }
        }
    }
}
