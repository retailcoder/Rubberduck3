using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Workspace;
using Rubberduck.UI.Windows;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Rubberduck.UI.Shell.Tools.WorkspaceExplorer
{
    public interface IWorkspaceExplorerViewModel : IToolWindowViewModel
    {
        void Load(ProjectFile workspace);
        IWorkspaceTreeNode? Selection { get; set; }
        ObservableCollection<IWorkspaceViewModel> Workspaces { get; }

        ICommand OpenDocumentCommand { get; }

        bool ShowFileExtensions { get; set; }
        bool ShowAllFiles { get; set; }
    }
}
