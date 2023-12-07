﻿using Rubberduck.UI.Services.WorkspaceExplorer;
using Rubberduck.UI.Windows;
using System.Collections.Generic;

namespace Rubberduck.UI.WorkspaceExplorer
{
    public interface IWorkspaceExplorerViewModel : IToolWindowViewModel
    {
        new string Title { get; }
        WorkspaceTreeNodeViewModel? Selection { get; set; }
        IWorkspaceUriInfo? SelectionInfo { get; }
        bool HasSelectionInfo { get; }
        ICollection<WorkspaceViewModel> Workspaces { get; }
    }
}
