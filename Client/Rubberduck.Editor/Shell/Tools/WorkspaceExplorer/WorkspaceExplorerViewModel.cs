using Rubberduck.Editor.Commands;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Workspace;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using Rubberduck.InternalApi.Services;
using Rubberduck.InternalApi.Settings;
using Rubberduck.InternalApi.Settings.Model.Editor.Tools;
using Rubberduck.ServerPlatform.Model.Telemetry;
using Rubberduck.UI;
using Rubberduck.UI.Command.SharedHandlers;
using Rubberduck.UI.Services.Abstract;
using Rubberduck.UI.Shell;
using Rubberduck.UI.Shell.Tools.WorkspaceExplorer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Abstractions;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace Rubberduck.Editor.Shell.Tools.WorkspaceExplorer
{
    public class WorkspaceExplorerViewModel : ToolWindowViewModelBase, IWorkspaceExplorerViewModel, ICommandBindingProvider
    {
        private readonly IAppWorkspacesService _service;

        public WorkspaceExplorerViewModel(RubberduckSettingsProvider settingsProvider,
            IAppWorkspacesService service, 
            ShowRubberduckSettingsCommand showSettingsCommand, 
            CloseToolWindowCommand closeToolwindowCommand,
            OpenDocumentCommand openDocumentCommand)
            : base(DockingLocation.DockLeft, showSettingsCommand, closeToolwindowCommand)
        {
            Title = "Workspace Explorer"; // TODO localize

            _service = service;
            Workspaces = new(service.ProjectFiles.Select(workspace => WorkspaceViewModel.FromModel(workspace, _service)));
            OpenDocumentCommand = openDocumentCommand;
            
            _dispatcher = Application.Current.Dispatcher;
            
            SettingKey = nameof(WorkspaceExplorerSettings);

            service.WorkspaceOpened += OnWorkspaceOpened;
            service.WorkspaceClosed += OnWorkspaceClosed;

            IsPinned = !settingsProvider.Settings.EditorSettings.ToolsSettings.WorkspaceExplorerSettings.AutoHide;

            CommandBindings = [
                new CommandBinding(WorkspaceExplorerCommands.OpenFileCommand, openDocumentCommand.ExecutedRouted(), openDocumentCommand.CanExecuteRouted()),
                new CommandBinding(WorkspaceExplorerCommands.IncludeFileCommand, ExecuteIncludeUriCommand),
                new CommandBinding(WorkspaceExplorerCommands.ExcludeFileCommand, ExecuteExcludeUriCommand),
            ];
        }

        public override IEnumerable<CommandBinding> CommandBindings { get; }

        public ICommand OpenDocumentCommand { get; }

        private Dispatcher _dispatcher;

        private void ExecuteIncludeUriCommand(object sender, ExecutedRoutedEventArgs e)
        {
            ProjectFile? project = default;
            WorkspaceUri? uri = default;

            if (e.Parameter is WorkspaceFileUri fileUri)
            {
                // TODO prompt for confirmation
                project = _service.ProjectFiles.SingleOrDefault(e => e.Uri == fileUri.WorkspaceRoot);
                if (project != null)
                {
                    var file = File.FromWorkspaceUri(fileUri);
                    if (file.HasSourceFileExtension(SupportedLanguage.VBA) || file.HasSourceFileExtension(SupportedLanguage.VB6))
                    {
                        var module = new Module { Name = fileUri.Name, Uri = fileUri.AbsoluteLocation.LocalPath };
                        project.VBProject.Modules = project.VBProject.Modules.Append(module).ToArray();
                    }
                    else
                    {
                        project.VBProject.OtherFiles = project.VBProject.OtherFiles.Append(file).ToArray();
                    }
                    uri = file.GetWorkspaceUri(fileUri.WorkspaceRoot);
                }
            }
            else if (e.Parameter is WorkspaceFolderUri folderUri)
            {
                // TODO prompt for confirmation for folder uri: AcceptFolderOnly | AcceptRecursiveContent
                project = _service.ProjectFiles.SingleOrDefault(e => e.Uri == folderUri.WorkspaceRoot);
                if (project != null)
                {
                    var folder = Folder.FromWorkspaceUri(folderUri);
                    project.VBProject.Folders = project.VBProject.Folders.Append(folder).ToArray();
                }
            }

            if (uri != null)
            {
                var workspace = Workspaces.SingleOrDefault(e => e.Uri.SourceRoot.LocalPath == uri.SourceRoot.LocalPath);
                
                if (workspace is WorkspaceViewModel vm)
                {
                    vm.IncludeInProject(uri);
                }
            }
        }

        private void ExecuteExcludeUriCommand(object sender, ExecutedRoutedEventArgs e)
        {
            // TODO prompt for confirmation

            ProjectFile? project = default;
            WorkspaceUri? uri = default;

            if (e.Parameter is WorkspaceFileUri fileUri)
            {
                project = _service.ProjectFiles.SingleOrDefault(e => e.Uri == fileUri.WorkspaceRoot);
                if (project != null)
                {
                    var file = project.VBProject.OtherFiles.SingleOrDefault(e => e.Uri == fileUri.AbsoluteLocation.LocalPath);
                    var srcFile = project.VBProject.Modules.SingleOrDefault(e => e.Uri == fileUri.AbsoluteLocation.LocalPath);

                    if (file != null)
                    {
                        var files = project.VBProject.OtherFiles.Except([file]);
                        project.VBProject.OtherFiles = files.ToArray();
                        uri = fileUri;
                    }
                    else if (srcFile != null)
                    {
                        var srcFiles = project.VBProject.Modules.Except([srcFile]);
                        project.VBProject.Modules = srcFiles.ToArray();
                        uri = fileUri;
                    }
                }
            }
            else if (e.Parameter is WorkspaceFolderUri folderUri)
            {
                project = _service.ProjectFiles.SingleOrDefault(e => e.Uri == folderUri.WorkspaceRoot);
                if (project != null)
                {
                    var folder = project.VBProject.Folders.SingleOrDefault(e => e.Uri == folderUri.AbsoluteLocation.LocalPath);
                    if (folder != null)
                    {
                        var folders = project.VBProject.Folders.Except([folder]);
                        project.VBProject.Folders = folders.ToArray();
                        uri = folderUri;
                    }
                }
            }

            if (uri != null)
            {
                var workspace = Workspaces.SingleOrDefault(e => e.Uri.WorkspaceRoot == uri.WorkspaceRoot);
                if (workspace is WorkspaceViewModel vm)
                {
                    vm.ExcludeFromProject(uri);
                }
            }
        }

        private void OnWorkspaceClosed(object? sender, WorkspaceServiceEventArgs e)
        {
            var item = Workspaces.SingleOrDefault(workspace => workspace.Uri == e.Uri);
            if (item != null)
            {
                _dispatcher.Invoke(() => Workspaces.Remove(item));
            }
        }

        private void OnWorkspaceOpened(object? sender, WorkspaceServiceEventArgs e)
        {
            var project = _service.ProjectFiles.SingleOrDefault(file => file.Uri.LocalPath == e.Uri.LocalPath);
            if (project != null)
            {
                Load(project);
            }
        }

        private IWorkspaceTreeNode? _selection;
        public IWorkspaceTreeNode? Selection
        {
            get => _selection;
            set
            {
                if (_selection != value)
                {
                    _selection = value;
                    OnPropertyChanged();
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
                    foreach (var workspace in Workspaces)
                    {
                        workspace.ShowFileExtensions = _showFileExtensions;
                    }
                    OnPropertyChanged();
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
                    foreach (var workspace in Workspaces)
                    {
                        workspace.ShowAllFiles = _showAllFiles;
                    }
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<IWorkspaceViewModel> Workspaces { get; } = [];

        public void Load(ProjectFile project)
        {
            var workspace = WorkspaceViewModel.FromModel(project, _service);
            _dispatcher.Invoke(() =>
            {
                Workspaces.Add(workspace);
            });
        }
    }
}
