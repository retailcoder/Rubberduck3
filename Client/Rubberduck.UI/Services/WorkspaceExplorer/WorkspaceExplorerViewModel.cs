﻿using Rubberduck.InternalApi.Model.Workspace;
using Rubberduck.SettingsProvider.Model.Tools;
using Rubberduck.UI.Command.SharedHandlers;
using Rubberduck.UI.Services.Abstract;
using Rubberduck.UI.WorkspaceExplorer;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace Rubberduck.UI.Services.WorkspaceExplorer
{
    public class WorkspaceExplorerViewModel : ViewModelBase, IWorkspaceExplorerViewModel
    {
        private readonly IWorkspaceService _service;

        public WorkspaceExplorerViewModel()
        {
            /* DESIGNER */
        }

        public WorkspaceExplorerViewModel(IWorkspaceService service, ShowRubberduckSettingsCommand showSettingsCommand)
        {
            _service = service;
            ShowSettingsCommand = showSettingsCommand;
            Workspaces = new(service.ProjectFiles.Select(workspace => WorkspaceViewModel.FromModel(workspace, _service)));
        }

        public string Title { get; set; } = "Workspace Explorer"; // TODO localize

        public ICommand ShowSettingsCommand { get; }
        public bool ShowPinButton { get; } = true;
        public bool ShowGearButton { get; } = true;

        public string SettingKey { get; } = nameof(WorkspaceExplorerSettings);

        private WorkspaceTreeNodeViewModel? _selection;
        public WorkspaceTreeNodeViewModel? Selection
        {
            get => _selection;
            set
            {
                if (_selection != value)
                {
                    _selection = value;
                    OnPropertyChanged();
                    SelectionInfo = _selection;
                }
            }
        }

        private IWorkspaceUriInfo? _selectionInfo;

        public IWorkspaceUriInfo? SelectionInfo
        {
            get => _selectionInfo;
            private set
            {
                if (_selectionInfo != value)
                {
                    _selectionInfo = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasSelectionInfo));
                }
            }
        }

        public bool HasSelectionInfo => _selectionInfo != null;

        public ObservableCollection<WorkspaceViewModel> Workspaces { get; } = new();

        public DockingLocation DockingLocation { get; set; } = DockingLocation.DockLeft;
        public object Header
        {
            get => Title;
            set
            {
                if (Title != value?.ToString())
                {
                    Title = value?.ToString() ?? string.Empty;
                    OnPropertyChanged();
                }
            }
        }

        private object _control;
        public object Content
        {
            get => _control;
            set
            {
                if (_control != value) 
                {
                    _control = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isSelected = true;
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

        private bool _isPinned;
        public bool IsPinned
        {
            get => _isPinned;
            set
            {
                if (_isPinned != value)
                {
                    _isPinned = value;
                    OnPropertyChanged();
                }
            }
        }

        public void Load(ProjectFile workspace)
        {
            Workspaces.Add(WorkspaceViewModel.FromModel(workspace, _service));
        }
    }
}
