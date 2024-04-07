using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Workspace;
using Rubberduck.UI.Shell.Tools.WorkspaceExplorer;
using System;

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

        public override string DisplayName => IsWithFileExtension 
            ? ((WorkspaceFileUri)Uri).FileName
            : ((WorkspaceFileUri)Uri).FileNameWithoutExtension;

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

        private bool _isWithFileExtension;
        public bool IsWithFileExtension
        {
            get => _isWithFileExtension;
            set
            {
                if (_isWithFileExtension != value)
                {
                    _isWithFileExtension = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }
    }
}
