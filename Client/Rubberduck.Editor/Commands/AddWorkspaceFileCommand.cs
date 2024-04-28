using Rubberduck.Editor.Shell.Dialogs.AddWorkspaceFile;
using Rubberduck.Editor.Shell.Tools.WorkspaceExplorer;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.UI.Command.Abstract;
using Rubberduck.UI.Services;
using Rubberduck.UI.Shell.Tools.WorkspaceExplorer;
using System.Linq;
using System.Threading.Tasks;

namespace Rubberduck.Editor.Commands;

public class AddWorkspaceFileCommand : CommandBase
{
    private readonly IAddWorkspaceFileService _addWorkspaceFileService;

    public AddWorkspaceFileCommand(UIServiceHelper service, IAddWorkspaceFileService workspaceFiles)
        : base(service)
    {
        _addWorkspaceFileService = workspaceFiles;
    }

    protected override Task OnExecuteAsync(object? parameter)
    {
        if (parameter is IWorkspaceFolderViewModel parent)
        {
            _addWorkspaceFileService.ConfigureModel = vm =>
            {
                vm.ParentFolderUri = (WorkspaceFolderUri)parent.Uri;
                vm.Selection = vm.Categories.First();
                vm.SelectedTemplate = vm.Templates.First();
            };

            if (_addWorkspaceFileService.ShowDialog(out var model))
            {
                var fileNode = WorkspaceFileViewModel.FromModel(new InternalApi.Model.Workspace.File { RelativeUri = model.WorkspaceFileUri.RelativeUriString! }, model.ParentFolderUri.WorkspaceRoot);
                // FIXME update this from UI thread
                //parent.AddChildNode(fileNode);
            }
        }
        return Task.CompletedTask;
    }
}
