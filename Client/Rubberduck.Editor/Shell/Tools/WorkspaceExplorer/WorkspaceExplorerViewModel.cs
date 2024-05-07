using Rubberduck.Editor.Commands;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Workspace;
using Rubberduck.InternalApi.Services;
using Rubberduck.InternalApi.Services.IO.Abstract;
using Rubberduck.InternalApi.Settings;
using Rubberduck.InternalApi.Settings.Model.Editor.Tools;
using Rubberduck.UI;
using Rubberduck.UI.Command.Abstract;
using Rubberduck.UI.Command.SharedHandlers;
using Rubberduck.UI.Command.StaticRouted;
using Rubberduck.UI.Services;
using Rubberduck.UI.Services.Abstract;
using Rubberduck.UI.Shell.Tools.WorkspaceExplorer;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Rubberduck.Editor.Shell.Tools.WorkspaceExplorer
{
    public class WorkspaceExplorerViewModel : ToolWindowViewModelBase, IWorkspaceExplorerViewModel, ICommandBindingProvider
    {
        private readonly IAppWorkspacesService _service;
        private readonly IWorkspaceIOServices _ioServices;
        private readonly WorkspaceExplorerCommandHandlers _handlers;
        private readonly RenameUriCommand _renameUriCommand;

        public WorkspaceExplorerViewModel(RubberduckSettingsProvider settingsProvider,
            IAppWorkspacesService service, 
            IWorkspaceIOServices ioServices,
            WorkspaceExplorerCommandHandlers handlers,
            ShowRubberduckSettingsCommand showSettingsCommand, 
            CloseToolWindowCommand closeToolwindowCommand,
            OpenDocumentCommand openDocumentCommand,
            RenameUriCommand renameUriCommand)
            : base(DockingLocation.DockLeft, showSettingsCommand, closeToolwindowCommand)
        {
            Title = "Workspace Explorer"; // TODO localize

            _service = service;
            _ioServices = ioServices;

            _handlers = handlers;
            _renameUriCommand = renameUriCommand;

            Workspaces = new(service.ProjectFiles.Select(workspace => WorkspaceViewModel.FromModel(workspace, ioServices)));
            OpenDocumentCommand = openDocumentCommand;
            
            _dispatcher = Application.Current.Dispatcher;
            
            SettingKey = nameof(WorkspaceExplorerSettings);

            service.WorkspaceOpened += OnWorkspaceOpened;
            service.WorkspaceClosed += OnWorkspaceClosed;

            IsPinned = !settingsProvider.Settings.EditorSettings.ToolsSettings.WorkspaceExplorerSettings.AutoHide;

            var expandFolderCommand = new DelegateCommand(UIServiceHelper.Instance!, 
                parameter =>
                {
                    if (parameter is IWorkspaceFolderViewModel folder) 
                    {
                        folder.IsExpanded = true;
                    }
                }, parameter => parameter is IWorkspaceFolderViewModel folder && !folder.IsExpanded);
            var collapseFolderCommand = new DelegateCommand(UIServiceHelper.Instance!,
                parameter =>
                {
                    if (parameter is IWorkspaceFolderViewModel folder)
                    {
                        folder.IsExpanded = false;
                    }
                }, parameter => parameter is IWorkspaceFolderViewModel folder && folder.IsExpanded);

            var prepareRenameUriCommand = new DelegateCommand(UIServiceHelper.Instance!,
                parameter =>
                {
                    var node = this.Workspaces.Select(e => e.FindChildNode((WorkspaceUri)parameter!)).SingleOrDefault();
                    if (node != null)
                    {
                        node.IsEditingName = true;
                        node.NameEditCompleted += OnNameEditCompleted;
                    }
                });

            CommandBindings = [
                new CommandBinding(WorkspaceExplorerCommands.OpenFileCommand, openDocumentCommand.ExecutedRouted(), openDocumentCommand.CanExecuteRouted()),
                new CommandBinding(WorkspaceExplorerCommands.IncludeFileCommand, ((CommandBase)_handlers.IncludeUriCommand).ExecutedRouted(), ((CommandBase)_handlers.IncludeUriCommand).CanExecuteRouted()),
                new CommandBinding(WorkspaceExplorerCommands.ExcludeFileCommand, ((CommandBase)_handlers.ExcludeUriCommand).ExecutedRouted(), ((CommandBase)_handlers.ExcludeUriCommand).CanExecuteRouted()),
                new CommandBinding(WorkspaceExplorerCommands.CreateFileCommand, ((CommandBase)_handlers.AddWorkspaceFileCommand).ExecutedRouted(), ((CommandBase)_handlers.AddWorkspaceFileCommand).CanExecuteRouted()),
                new CommandBinding(WorkspaceExplorerCommands.CreateFolderCommand, ((CommandBase)_handlers.CreateFolderCommand).ExecutedRouted(), ((CommandBase)_handlers.CreateFolderCommand).CanExecuteRouted()),
                new CommandBinding(WorkspaceExplorerCommands.DeleteUriCommand, ((CommandBase)_handlers.DeleteUriCommand).ExecutedRouted(), ((CommandBase)_handlers.DeleteUriCommand).CanExecuteRouted()),
                new CommandBinding(WorkspaceExplorerCommands.RenameUriCommand, prepareRenameUriCommand.ExecutedRouted()),
                new CommandBinding(WorkspaceExplorerCommands.ExpandFolderCommand, expandFolderCommand.ExecutedRouted(), expandFolderCommand.CanExecuteRouted()),
                new CommandBinding(WorkspaceExplorerCommands.CollapseFolderCommand, collapseFolderCommand.ExecutedRouted(), collapseFolderCommand.CanExecuteRouted()),
                new CommandBinding(WorkspaceExplorerCommands.NewProjectCommand,((CommandBase)_handlers.NewProjectCommand).ExecutedRouted()),
                new CommandBinding(WorkspaceExplorerCommands.OpenProjectWorkspaceCommand,((CommandBase)_handlers.OpenProjectCommand).ExecutedRouted()),
                new CommandBinding(WorkspaceExplorerCommands.CloseProjectWorkspaceCommand,((CommandBase)_handlers.CloseWorkspaceCommand).ExecutedRouted(),((CommandBase)_handlers.CloseWorkspaceCommand).CanExecuteRouted()),
                new CommandBinding(WorkspaceExplorerCommands.SynchronizeProjectWorkspaceCommand,((CommandBase)_handlers.SynchronizeWorkspaceCommand).ExecutedRouted(),((CommandBase)_handlers.SynchronizeWorkspaceCommand).CanExecuteRouted()),
                new CommandBinding(WorkspaceExplorerCommands.RenameProjectWorkspaceCommand),
                new CommandBinding(WorkspaceExplorerCommands.OpenFolderInWindowsExplorerCommand, ((CommandBase)_handlers.OpenUriInWindowsExplorerCommand).ExecutedRouted(), ((CommandBase)_handlers.OpenUriInWindowsExplorerCommand).CanExecuteRouted())
            ];
        }

        private void OnNameEditCompleted(object? sender, CancelEventArgs e)
        {
            if (sender is IWorkspaceTreeNode node)
            {
                node.NameEditCompleted -= OnNameEditCompleted;
                _renameUriCommand.Execute(node);
            }
        }

        public IEnumerable<object> ContextMenuItems => new object[]
            {
                new MenuItem { Command = FileCommands.NewProjectCommand },
                new MenuItem { Command = FileCommands.OpenProjectWorkspaceCommand },
                new Separator(),
                new MenuItem { Command = SettingCommands.ShowSettingsCommand, CommandParameter = SettingKey }
            };

        public override IEnumerable<CommandBinding> CommandBindings { get; }

        public ICommand OpenDocumentCommand { get; }

        private Dispatcher _dispatcher;


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
            var workspace = WorkspaceViewModel.FromModel(project, _ioServices);
            _dispatcher.Invoke(() =>
            {
                Workspaces.Add(workspace);
            });
        }
    }
}
