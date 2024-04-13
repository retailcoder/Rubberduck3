using Rubberduck.Editor.Commands;
using Rubberduck.UI.Command;
using Rubberduck.UI.Command.Abstract;
using Rubberduck.UI.Shell.Tools.WorkspaceExplorer;
using System.Collections.Generic;
using System.Windows.Input;

namespace Rubberduck.Editor.Shell.Tools.WorkspaceExplorer
{
    public class WorkspaceExplorerCommandHandlers : CommandHandlers
    {
        public ICommand NewProjectCommand { get; init; }
        public ICommand OpenProjectCommand { get; init; }
        public ICommand DeleteUriCommand { get; init; }
        public ICommand OpenDocumentCommand { get; init; }
        public ICommand CloseDocumentCommand { get; init; }
        public ICommand CloseWorkspaceCommand { get; init; }
        public ICommand SynchronizeWorkspaceCommand { get; init; }
        public ICommand OpenUriInWindowsExplorerCommand { get; init; }

        public ICommand IncludeUriCommand { get; init; }
        public ICommand ExcludeUriCommand { get; init; }

        public WorkspaceExplorerCommandHandlers(
            NewProjectCommand newProjectCommand,
            OpenProjectCommand openProjectCommand,
            DeleteUriCommand deleteUriCommand,
            OpenDocumentCommand openDocumentCommand,
            CloseDocumentCommand closeDocumentCommand,
            CloseWorkspaceCommand closeWorkspaceCommand,
            SynchronizeWorkspaceCommand synchronizeWorkspaceCommand,
            OpenUriInWindowsExplorerCommand openUriInWindowsExplorerCommand,
            IncludeInProjectCommand includeUriCommand,
            ExcludeFromProjectCommand excludeUriCommand)
        {
            NewProjectCommand = newProjectCommand;
            OpenProjectCommand = openProjectCommand;
            DeleteUriCommand = deleteUriCommand;
            OpenDocumentCommand = openDocumentCommand;
            CloseDocumentCommand = closeDocumentCommand;
            CloseWorkspaceCommand = closeWorkspaceCommand;
            SynchronizeWorkspaceCommand = synchronizeWorkspaceCommand;
            OpenUriInWindowsExplorerCommand = openUriInWindowsExplorerCommand;
            IncludeUriCommand = includeUriCommand;
            ExcludeUriCommand = excludeUriCommand;
        }

        public override IEnumerable<CommandBinding> CreateCommandBindings() =>
            Bind(
                (WorkspaceExplorerCommands.OpenFileCommand, OpenDocumentCommand)
            );
    }
}
