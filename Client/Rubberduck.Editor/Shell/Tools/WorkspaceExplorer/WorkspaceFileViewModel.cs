using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Workspace;
using Rubberduck.UI.Shell.Tools.WorkspaceExplorer;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Rubberduck.Editor.Shell.Tools.WorkspaceExplorer
{
    public class WorkspaceFileViewModel : WorkspaceTreeNodeViewModel, IWorkspaceFileViewModel
    {
        public static WorkspaceFileViewModel FromModel(File model, Uri workspaceRoot)
        {
            return new WorkspaceFileViewModel
            {
                Uri = new WorkspaceFileUri(model.Uri, workspaceRoot),
                Name = model.Name,
                IsAutoOpen = model.IsAutoOpen,
                IsInProject = true
            };
        }

        public override IEnumerable<object> ContextMenuItems => IsInProject
            ? new object[] 
                {
                    new MenuItem { Command = WorkspaceExplorerCommands.OpenFileCommand, CommandParameter = Uri },
                    new MenuItem { Command = WorkspaceExplorerCommands.RenameUriCommand, CommandParameter = Uri },
                    new MenuItem { Command = WorkspaceExplorerCommands.DeleteUriCommand, CommandParameter = Uri },
                    new Separator(),
                    new MenuItem { Command = WorkspaceExplorerCommands.ExcludeFileCommand, CommandParameter = Uri },
                }
            : new object[]
                {
                    new MenuItem { Command = WorkspaceExplorerCommands.RenameUriCommand, CommandParameter = Uri },
                    new MenuItem { Command = WorkspaceExplorerCommands.DeleteUriCommand, CommandParameter = Uri },
                    new Separator(),
                    new MenuItem { Command = WorkspaceExplorerCommands.IncludeFileCommand, CommandParameter = Uri },
                };

        public override string DisplayName 
        {
            get => ShowFileExtensions
            ? ((WorkspaceFileUri)Uri).FileName
            : ((WorkspaceFileUri)Uri).FileNameWithoutExtension;

            set
            {
                Name = value;
            }
        } 

        private bool _isAutoOpen;
        public bool IsAutoOpen
        {
            get => _isAutoOpen;
            set
            {
                if (_isAutoOpen != value)
                {
                    _isAutoOpen = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isReadOnly;
        public bool IsReadOnly
        {
            get => _isReadOnly;
            set
            {
                if (_isReadOnly != value)
                {
                    _isReadOnly = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
