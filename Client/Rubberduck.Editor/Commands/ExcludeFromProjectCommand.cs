using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Workspace;
using Rubberduck.InternalApi.Services;
using Rubberduck.UI.Command.Abstract;
using Rubberduck.UI.Services;
using System.Linq;
using System.Threading.Tasks;

namespace Rubberduck.Editor.Commands;

public class ExcludeFromProjectCommand : CommandBase
{
    private readonly IAppWorkspacesService _workspaces;
    private readonly IProjectFileService _projectFile;

    public ExcludeFromProjectCommand(UIServiceHelper service, IAppWorkspacesService workspaces, IProjectFileService projectFile)
        : base(service)
    {
        _workspaces = workspaces;
        _projectFile = projectFile;
    }

    protected override Task OnExecuteAsync(object? parameter)
    {
        ProjectFile? project = default;
        WorkspaceUri? uri = default;

        if (parameter is WorkspaceFileUri fileUri)
        {
            project = _workspaces.ProjectFiles.SingleOrDefault(e => e.Uri == fileUri.WorkspaceRoot);
            if (project != null)
            {
                var file = project.VBProject.OtherFiles.SingleOrDefault(e => e.Uri == fileUri.RelativeUriString);
                var srcFile = project.VBProject.Modules.SingleOrDefault(e => e.Uri == fileUri.RelativeUriString);

                if (file != null)
                {
                    var files = project.VBProject.OtherFiles.Except([file]);
                    project.VBProject.OtherFiles = files.ToArray();
                    uri = fileUri;
                }
                else if (srcFile != null)
                {
                    var srcFiles = project.VBProject.Modules.Except([srcFile]);
                    project.VBProject.Modules = srcFiles.ToArray();
                    uri = fileUri;
                }

                var workspace = _workspaces.Workspaces.Workspaces.SingleOrDefault(e => e.WorkspaceRoot?.SourceRoot.LocalPath == fileUri.SourceRoot.LocalPath);
                if (workspace != null)
                {
                    workspace.DeleteWorkspaceUri(fileUri);
                    _projectFile.WriteFile(project);
                }
            }
        }
        else if (parameter is WorkspaceFolderUri folderUri)
        {
            project = _workspaces.ProjectFiles.SingleOrDefault(e => e.Uri == folderUri.WorkspaceRoot);
            if (project != null)
            {
                var folder = project.VBProject.Folders.SingleOrDefault(e => e.Uri == "\\" + folderUri.RelativeUriString?.Replace("/", "\\"));
                if (folder != null)
                {
                    var folders = project.VBProject.Folders.Except([folder]);
                    project.VBProject.Folders = folders.ToArray();
                    uri = folderUri;
                }

                var workspace = _workspaces.Workspaces.Workspaces.SingleOrDefault(e => e.WorkspaceRoot?.SourceRoot.LocalPath == folderUri.SourceRoot.LocalPath);
                if (workspace != null)
                {
                    workspace.DeleteWorkspaceUri(folderUri);
                    _projectFile.WriteFile(project);
                }
            }
        }

        return Task.CompletedTask;
    }
}

