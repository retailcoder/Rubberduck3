using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Workspace;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using Rubberduck.InternalApi.Services;
using Rubberduck.UI.Command.Abstract;
using Rubberduck.UI.Services;
using System.Linq;
using System.Threading.Tasks;

namespace Rubberduck.Editor.Commands;

public class IncludeInProjectCommand : CommandBase
{
    private readonly IAppWorkspacesService _workspaces;
    private readonly IProjectFileService _projectFile;

    public IncludeInProjectCommand(UIServiceHelper service, IAppWorkspacesService workspaces, IProjectFileService projectFile) 
        : base(service)
    {
        _workspaces = workspaces;
        _projectFile = projectFile;
    }

    protected override Task OnExecuteAsync(object? parameter)
    {
        ProjectFile? project = default;

        if (parameter is WorkspaceFileUri fileUri)
        {
            // TODO prompt for confirmation
            project = _workspaces.ProjectFiles.SingleOrDefault(e => e.Uri == fileUri.WorkspaceRoot);
            if (project != null)
            {
                var file = InternalApi.Model.Workspace.File.FromWorkspaceUri(fileUri);
                file.IsLoadError = !System.IO.File.Exists(fileUri.AbsoluteLocation.LocalPath);
                if (file.HasSourceFileExtension(SupportedLanguage.VBA) || file.HasSourceFileExtension(SupportedLanguage.VB6))
                {
                    var module = new Module { Name = fileUri.Name, RelativeUri = fileUri.AbsoluteLocation.LocalPath, IsLoadError = file.IsLoadError };
                    project.VBProject.Modules = project.VBProject.Modules.Append(module).ToArray();
                }
                else
                {
                    project.VBProject.OtherFiles = project.VBProject.OtherFiles.Append(file).ToArray();
                }

                var workspace = _workspaces.Workspaces.Workspaces.SingleOrDefault(e => e.WorkspaceRoot?.SourceRoot.LocalPath == fileUri.SourceRoot.LocalPath);
                if (workspace != null)
                {
                    _projectFile.WriteFile(project);
                }
            }
        }
        else if (parameter is WorkspaceFolderUri folderUri)
        {
            // TODO prompt for confirmation for folder uri: AcceptFolderOnly | AcceptRecursiveContent
            project = _workspaces.ProjectFiles.SingleOrDefault(e => e.Uri == folderUri.WorkspaceRoot);
            if (project != null)
            {
                var folder = Folder.FromWorkspaceUri(folderUri);
                folder.IsLoadError = !System.IO.Directory.Exists(folderUri.AbsoluteLocation.LocalPath);
                project.VBProject.Folders = project.VBProject.Folders.Append(folder.RelativeUri).ToArray();

                var workspace = _workspaces.Workspaces.Workspaces.SingleOrDefault(e => e.WorkspaceRoot?.SourceRoot.LocalPath == folderUri.SourceRoot.LocalPath);
                if (workspace != null)
                {
                    _projectFile.WriteFile(project);
                }
            }
        }

        return Task.CompletedTask;
    }
}

