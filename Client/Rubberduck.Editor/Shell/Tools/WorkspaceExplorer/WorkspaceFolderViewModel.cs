using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Workspace;
using Rubberduck.UI.Shell.Tools.WorkspaceExplorer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace Rubberduck.Editor.Shell.Tools.WorkspaceExplorer
{
    public class WorkspaceFolderViewModel : WorkspaceTreeNodeViewModel, IWorkspaceFolderViewModel
    {
        public static WorkspaceFolderViewModel FromModel(Folder model, Uri workspaceRoot)
        {
            return new WorkspaceFolderViewModel
            {
                Uri = new WorkspaceFolderUri(model.RelativeUri, workspaceRoot),
                Name = model.Name,
                IsInProject = true,
                IsLoadError = model.IsLoadError,
            };
        }

        public override IEnumerable<object> ContextMenuItems => IsInProject
            ? new object[]
                {
                    new MenuItem { Command = WorkspaceExplorerCommands.CreateFileCommand, CommandParameter = this },
                    new MenuItem { Command = WorkspaceExplorerCommands.CreateFolderCommand, CommandParameter = this },
                    new Separator(),
                    new MenuItem { Command = WorkspaceExplorerCommands.ExpandFolderCommand, CommandParameter = this },
                    new MenuItem { Command = WorkspaceExplorerCommands.CollapseFolderCommand, CommandParameter = this },
                    new Separator(),
                    new MenuItem { Command = WorkspaceExplorerCommands.OpenFolderInWindowsExplorerCommand, CommandParameter = Uri },
                    new MenuItem { Command = WorkspaceExplorerCommands.RenameUriCommand, CommandParameter = Uri },
                    new MenuItem { Command = WorkspaceExplorerCommands.DeleteUriCommand, CommandParameter = this },
                    new Separator(),
                    new MenuItem { Command = WorkspaceExplorerCommands.ExcludeFileCommand, CommandParameter = Uri },
                }
            : new object[]
                {
                    new MenuItem { Command = WorkspaceExplorerCommands.ExpandFolderCommand, CommandParameter = this },
                    new MenuItem { Command = WorkspaceExplorerCommands.CollapseFolderCommand, CommandParameter = this },
                    new Separator(),
                    new MenuItem { Command = WorkspaceExplorerCommands.OpenFolderInWindowsExplorerCommand, CommandParameter = Uri },
                    new MenuItem { Command = WorkspaceExplorerCommands.RenameUriCommand, CommandParameter = Uri },
                    new MenuItem { Command = WorkspaceExplorerCommands.DeleteUriCommand, CommandParameter = Uri },
                    new Separator(),
                    new MenuItem { Command = WorkspaceExplorerCommands.IncludeFileCommand, CommandParameter = Uri },
                };
    }
}
