using Rubberduck.InternalApi.Extensions;

namespace Rubberduck.UI.Shell.Tools.WorkspaceExplorer;

public interface IWorkspaceViewModel : IWorkspaceTreeNode
{
    IWorkspaceTreeNode FindChildNode(WorkspaceUri uri, IWorkspaceTreeNode? parent = null);

    bool IsFileSystemWatcherEnabled { get; set; }
    void RemoveWorkspaceUri(WorkspaceFileUri uri);
    void RemoveWorkspaceUri(WorkspaceFolderUri uri);
}
