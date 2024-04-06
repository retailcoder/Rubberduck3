using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Workspace;
using Rubberduck.UI.Shell.Tools.WorkspaceExplorer;
using System;
using System.Linq;

namespace Rubberduck.Editor.Shell.Tools.WorkspaceExplorer
{
    public class WorkspaceFolderViewModel : WorkspaceTreeNodeViewModel, IWorkspaceFolderViewModel
    {
        public static WorkspaceFolderViewModel FromModel(Folder model, Uri workspaceRoot)
        {
            return new WorkspaceFolderViewModel
            {
                Uri = new WorkspaceFolderUri(model.Uri, workspaceRoot),
                Name = model.Name.Split('.').Last(),
                IsInProject = true,
            };
        }
    }
}
