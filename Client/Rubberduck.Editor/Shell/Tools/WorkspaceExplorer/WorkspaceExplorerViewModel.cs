﻿using Rubberduck.Editor.Commands;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Workspace;
using Rubberduck.InternalApi.Services;
using Rubberduck.InternalApi.Settings;
using Rubberduck.InternalApi.Settings.Model.Editor.Tools;
using Rubberduck.UI;
using Rubberduck.UI.Command.SharedHandlers;
using Rubberduck.UI.Services.Abstract;
using Rubberduck.UI.Shell;
using Rubberduck.UI.Shell.Tools.WorkspaceExplorer;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Abstractions;
using System.Linq;
using System.Windows;
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
            ];
        }

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
