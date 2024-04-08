﻿using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Workspace;
using Rubberduck.InternalApi.Services;
using Rubberduck.UI;
using Rubberduck.UI.Shell.Tools.WorkspaceExplorer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Data;

namespace Rubberduck.Editor.Shell.Tools.WorkspaceExplorer
{
    public class WorkspaceViewModel : ViewModelBase, IWorkspaceTreeNode, IWorkspaceViewModel
    {
        public static WorkspaceViewModel FromModel(ProjectFile model, IAppWorkspacesService service)
        {
            var vm = new WorkspaceViewModel(service)
            {
                Name = model.VBProject.Name,
                DisplayName = model.VBProject.Name,
                Uri = new WorkspaceFolderUri(null, new Uri(service.FileSystem.Path.Combine(model.Uri.LocalPath, WorkspaceUri.SourceRootName + "\\"))),
                //IsFileSystemWatcherEnabled = service.IsFileSystemWatcherEnabled(model.Uri),
                IsExpanded = true
            };

            var sourceFiles = model.VBProject.Modules.Select(e => WorkspaceSourceFileViewModel.FromModel(e, model.Uri) as WorkspaceTreeNodeViewModel);
            var otherFiles = model.VBProject.OtherFiles.Select(e => WorkspaceFileViewModel.FromModel(e, model.Uri) as WorkspaceTreeNodeViewModel);
            var projectFolders = model.VBProject.Folders.Select(e => WorkspaceFolderViewModel.FromModel(e, model.Uri)).OrderBy(e => e.Name).ToList();
            var rootFolders = projectFolders.Where(e => ((WorkspaceFolderUri)e.Uri).RelativeUriString!.Count(c => c == '/') == 0);

            var projectFilesByFolder = sourceFiles.Concat(otherFiles)
                .GroupBy(e => ((WorkspaceFileUri)e.Uri).WorkspaceFolder.RelativeUriString ?? WorkspaceUri.SourceRootName)
                .ToDictionary(e => Path.TrimEndingDirectorySeparator(e.Key), e => e.AsEnumerable());

            foreach (var folder in rootFolders)
            {
                AddFolderContent(service, folder, projectFilesByFolder, projectFolders);
                vm.AddChildNode(folder);
            }

            var srcRoot = new WorkspaceFolderUri(null, model.Uri);
            if (projectFilesByFolder.TryGetValue(srcRoot.AbsoluteLocation.LocalPath, out var rootFolderFiles))
            {
                var rootFilePaths = rootFolderFiles.OrderBy(e => e.Name).Select(e => e.Uri.LocalPath).ToHashSet();
                AddFolderFileNodes(service, vm, rootFolderFiles, rootFilePaths);

                projectFilesByFolder.Remove(srcRoot.AbsoluteLocation.LocalPath);
            }

            foreach (var key in projectFilesByFolder.Keys)
            {
                var folder = CreateWorkspaceFolderNode(service, projectFilesByFolder[key], new WorkspaceFolderUri(key, model.Uri));
                vm.AddChildNode(folder);
            }

            return vm;
        }

        private static void AddFolderContent(IAppWorkspacesService service, WorkspaceFolderViewModel folder, IDictionary<string, IEnumerable<WorkspaceTreeNodeViewModel>> projectFilesByFolder, IEnumerable<WorkspaceFolderViewModel> projectFolders)
        {
            var workspaceFolderUri = (WorkspaceFolderUri)folder.Uri;
            var key = System.IO.Path.TrimEndingDirectorySeparator(workspaceFolderUri.RelativeUriString ?? WorkspaceUri.SourceRootName);

            var localPath = ((WorkspaceFolderUri)folder.Uri).AbsoluteLocation.LocalPath;
            foreach (var subFolder in projectFolders.Where(e => localPath == new System.IO.DirectoryInfo(new WorkspaceFolderUri(e.Uri.OriginalString, workspaceFolderUri.WorkspaceRoot).AbsoluteLocation.LocalPath).Parent!.FullName))
            {
                AddFolderContent(service, subFolder, projectFilesByFolder, projectFolders);
                folder.AddChildNode(subFolder);
            }

            var projectFolderPaths = projectFolders.Select(e => e.Uri.AbsoluteLocation.LocalPath).ToHashSet();
            foreach (var subFolder in Directory.EnumerateDirectories(localPath))
            {
                if (!projectFolderPaths.Contains(subFolder))
                {
                    var ghostUri = new WorkspaceFolderUri(subFolder, workspaceFolderUri.WorkspaceRoot);
                    var node = new WorkspaceFolderViewModel { IsInProject = false, Uri = ghostUri, Name = ghostUri.FolderName };
                    AddFolderContent(service, node, projectFilesByFolder, projectFolders);

                    folder.AddChildNode(node);
                    AddFolderFileNodes(service, node, [], []);
                }
            }

            // add the files last to keep folders together
            if (projectFilesByFolder.TryGetValue(key, out var projectFiles) && projectFiles is not null)
            {
                var projectFilePaths = projectFiles.Select(file => ((WorkspaceFileUri)file.Uri).AbsoluteLocation.LocalPath).ToHashSet();
                AddFolderFileNodes(service, folder, projectFiles, projectFilePaths);

                projectFilesByFolder.Remove(key);
            }
        }

        private static WorkspaceFolderViewModel CreateWorkspaceFolderNode(IAppWorkspacesService service, IEnumerable<IWorkspaceTreeNode> projectFiles, WorkspaceFolderUri uri)
        {
            var folder = new WorkspaceFolderViewModel
            {
                IsInProject = true,
                Uri = uri,
                Name = uri.FolderName
            };

            var projectFilePaths = projectFiles.Select(file => file.Uri.AbsoluteLocation.LocalPath).ToHashSet();
            AddFolderFileNodes(service, folder, projectFiles, projectFilePaths);
            return folder;
        }

        private static void AddFolderFileNodes(IAppWorkspacesService service, IWorkspaceTreeNode folder, IEnumerable<IWorkspaceTreeNode> projectFiles, HashSet<string> projectFilePaths)
        {
            var workspaceFiles = GetWorkspaceFilesNotInProject(service, folder, projectFilePaths);
            foreach (var file in projectFiles.Concat(workspaceFiles))
            {
                folder.AddChildNode(file);
            }
        }

        private static IEnumerable<WorkspaceFileViewModel> GetWorkspaceFilesNotInProject(IAppWorkspacesService service, IWorkspaceTreeNode folder, HashSet<string> projectFilePaths)
        {
            var workspaceRoot = ((WorkspaceFolderUri)folder.Uri).WorkspaceRoot;
            var results = service.FileSystem.Directory.GetFiles(((WorkspaceFolderUri)folder.Uri).AbsoluteLocation.LocalPath).Except(projectFilePaths)
                        .Select(file => new WorkspaceFileViewModel
                        {
                            Uri = new WorkspaceFileUri(file.Replace($"\\{WorkspaceUri.SourceRootName}", string.Empty)[workspaceRoot.LocalPath.Length..], workspaceRoot),
                            Name = service.FileSystem.Path.GetFileNameWithoutExtension(file),
                            IsInProject = false // file exists in a project folder but is not included in the project
                        });
            return results;
        }

        private readonly IAppWorkspacesService _workspaces;
        public ICollectionView ItemsViewSource { get; }

        public WorkspaceViewModel(IAppWorkspacesService workspaces)
        {
            _workspaces = workspaces;
            ItemsViewSource = CollectionViewSource.GetDefaultView(_children);
            ItemsViewSource.Filter = o => ShowAllFiles || ((IWorkspaceTreeNode)o).IsInProject;
        }

        private bool _isFileSystemWatcherEnabled;
        public bool IsFileSystemWatcherEnabled
        {
            get => _isFileSystemWatcherEnabled;
            set
            {
                if (_isFileSystemWatcherEnabled != value)
                {
                    _isFileSystemWatcherEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        public void IncludeInProject(WorkspaceUri uri)
        {
            var node = FindNode(uri, this);
            if (node != null)
            {
                node.IsInProject = true;
            }
        }

        private IWorkspaceTreeNode? FindNode(WorkspaceUri uri, IWorkspaceTreeNode root)
        {
            var target = uri.AbsoluteLocation.LocalPath;
            if (root.Uri.AbsoluteLocation.LocalPath == target)
            {
                return root;
            }

            foreach (var node in root.Children)
            {
                if (node.Uri.AbsoluteLocation.LocalPath == target)
                {
                    return node;
                }
                if (node.Children.Count > 0)
                {
                    foreach (var child in node.Children)
                    {
                        return FindNode(uri, child);
                    }
                }
            }

            return default;
        }

        public void ExcludeFromProject(WorkspaceUri uri)
        {
            var node = FindNode(uri, this);
            if (node != null)
            {
                node.IsInProject = false;
            }
        }

        private readonly ObservableCollection<IWorkspaceTreeNode> _children = new();
        public ObservableCollection<IWorkspaceTreeNode> Children => _children;

        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; }

        public bool IsEditingName { get; set; }

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
        public string FileName => Uri.GetComponents(UriComponents.Path, UriFormat.Unescaped).Split('/').Last();

        public void AddChildNode(IWorkspaceTreeNode childNode)
        {
            _children.Add(childNode);
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
    }
}
