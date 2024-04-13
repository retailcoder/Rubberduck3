using Rubberduck.InternalApi.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;

namespace Rubberduck.UI.Shell.Tools.WorkspaceExplorer
{
    public interface IWorkspaceFolderViewModel : IWorkspaceTreeNode
    {

    }

    public interface IWorkspaceTreeNode : INotifyPropertyChanged
    {
        WorkspaceUri Uri { get; }
        string FileName { get; }
        string DisplayName { get; set; }
        bool ShowFileExtensions { get; set; }
        bool ShowAllFiles { get; set; }
        ICollectionView ItemsViewSource { get; }
        ObservableCollection<IWorkspaceTreeNode> Children { get; }
        void AddChildNode(IWorkspaceTreeNode childNode);

        bool IsSelected { get; set; }
        bool Filtered { get; set; }
        bool IsExpanded { get; set; }
        bool IsEditingName { get; set; }

        bool IsInProject { get; set; }
        bool IsLoadError { get; set; }
        bool IsVisible { get; set; }

        IEnumerable<object> ContextMenuItems { get; }
    }
}
