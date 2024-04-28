using Rubberduck.InternalApi.Extensions;
using Rubberduck.UI;
using Rubberduck.UI.Shell.Tools.WorkspaceExplorer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Data;

namespace Rubberduck.Editor.Shell.Tools.WorkspaceExplorer
{
    public class WorkspaceTreeNodeViewModel : ViewModelBase, IWorkspaceTreeNode
    {
        public ICollectionView ItemsViewSource { get; }

        public event EventHandler<CancelEventArgs> NameEditCompleted = delegate { };

        public WorkspaceTreeNodeViewModel()
        {
            ItemsViewSource = CollectionViewSource.GetDefaultView(_children);
            ItemsViewSource.Filter = o => ShowAllFiles || ((IWorkspaceTreeNode)o).IsInProject;
            _editName = Name;
        }

        public virtual IEnumerable<object> ContextMenuItems => new object[]
            {
                new MenuItem { Command = WorkspaceExplorerCommands.CreateFileCommand },
                new MenuItem { Command = WorkspaceExplorerCommands.CreateFolderCommand },
                new Separator(),
                new MenuItem { Command = WorkspaceExplorerCommands.OpenFolderInWindowsExplorerCommand, CommandParameter = Uri },
            };


        private string _name = null!;
        public virtual string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                    DisplayName = _name;
                }
            }
        }

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

        private string _displayName;
        public virtual string DisplayName 
        {
            get => _displayName;
            set
            {
                if (_displayName != value)
                {
                    _displayName = value;
                    OnPropertyChanged();
                }
            }
        }

        private WorkspaceUri _uri = null!;
        public WorkspaceUri Uri
        {
            get => _uri;
            set
            {
                if (_uri != value)
                {
                    _uri = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _fileName = null!;
        public string FileName
        {
            get => System.IO.Path.GetFileNameWithoutExtension(_uri.AbsoluteLocation.LocalPath);
            set
            {
                if (_fileName != value)
                {
                    var oldValue = _fileName;
                    _fileName = value;
                    OnPropertyChanged();

                    var extension = _uri.AbsoluteLocation.LocalPath.Split(".").Last();
                    Uri = new WorkspaceFileUri(System.IO.Path.Combine(_uri.AbsoluteLocation.LocalPath[..^(oldValue?.Length ?? 0)] + "." + extension, value), _uri.WorkspaceRoot);
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

        private readonly ObservableCollection<IWorkspaceTreeNode> _children = [];
        public ObservableCollection<IWorkspaceTreeNode> Children => _children;


        private int _version;
        public int Version
        {
            get => _version;
            set
            {
                if (_version != value)
                {
                    _version = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isOpen;
        public bool IsOpen
        {
            get => _isOpen;
            set
            {
                if (_isOpen != value)
                {
                    _isOpen = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isFileWatcherEnabled;
        public bool IsFileWatcherEnabled
        {
            get => _isFileWatcherEnabled;
            set
            {
                if (_isFileWatcherEnabled != value)
                {
                    _isFileWatcherEnabled = value;
                    OnPropertyChanged();
                }
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

        public void AddChildNode(IWorkspaceTreeNode childNode)
        {
            _children.Add(childNode);
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

        private bool _isEditingName;
        public bool IsEditingName
        {
            get => _isEditingName;
            set
            {
                if (_isEditingName != value)
                {
                    _isEditingName = value;
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
                    foreach (var node in Children)
                    {
                        node.ShowFileExtensions = _showFileExtensions;
                    }
                    OnPropertyChanged();
                }
                ItemsViewSource.Refresh();
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
                }
                ItemsViewSource.Refresh();
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

        private bool _isDeleted = true;
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
}
