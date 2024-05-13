using AsyncAwaitBestPractices;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using Rubberduck.Editor.Shell.Tools.WorkspaceExplorer;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Services;
using Rubberduck.InternalApi.Services.IO.Abstract;
using Rubberduck.UI.Command.Abstract;
using Rubberduck.UI.Services;
using Rubberduck.UI.Shell.Tools.WorkspaceExplorer;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Rubberduck.Editor.Commands;

public class RenameUriCommand : CommandBase
{
    private readonly Lazy<ILanguageClient> _lsp;
    private readonly IAppWorkspacesService _workspaceServices;
    private readonly IWorkspaceIOServices _ioServices;

    public RenameUriCommand(UIServiceHelper service,
        IWorkspaceIOServices ioServices,
        IAppWorkspacesService workspaceService,
        Func<ILanguageClient> lsp) 
        : base(service)
    {
        _ioServices = ioServices;
        _lsp = new Lazy<ILanguageClient>(lsp);
        _workspaceServices = workspaceService;
    }

    protected async override Task OnExecuteAsync(object? parameter)
    {
        if (parameter is IWorkspaceFileViewModel fileNode)
        {
            var oldName = fileNode.FileName;
            var newName = fileNode.EditName;

            if (oldName != newName)
            {
                var oldUri = (WorkspaceFileUri)fileNode.Uri;
                fileNode.FileName = newName; // implementation updates the URI
                
                await Task.WhenAll(
                    UpdateProjectFileAsync(fileNode, oldUri),
                    _ioServices.WorkspaceFile.RenameAsync(oldUri, (WorkspaceFileUri)fileNode.Uri)).ConfigureAwait(false);

                if (fileNode.IsInProject)
                {
                    NotifyLanguageServer(oldUri, fileNode.Uri);
                }
            }
        }
        else if (parameter is IWorkspaceFolderViewModel folderNode)
        {
            var oldName = folderNode.FileName;
            var newName = folderNode.EditName;

            if (oldName != newName)
            {
                var oldUri = new WorkspaceFolderUri(folderNode.Uri.RelativeUriString, folderNode.Uri.WorkspaceRoot);
                folderNode.FileName = newName;

                var newUri = new WorkspaceFolderUri(folderNode.Uri.RelativeUriString, folderNode.Uri.WorkspaceRoot);

                foreach (var childNode in folderNode.Children)
                {
                    UpdateNodePath(childNode, oldUri, newUri);
                }

                await Task.WhenAll(
                    UpdateProjectFileAsync(folderNode, oldUri), 
                    _ioServices.WorkspaceFolder.RenameAsync(oldUri, newUri)).ConfigureAwait(false);

                if (folderNode.IsInProject)
                {
                    NotifyLanguageServer(oldUri, folderNode.Uri);
                }
            }
        }
    }

    private void UpdateNodePath(IWorkspaceTreeNode node, WorkspaceUri oldUri, WorkspaceFolderUri folder)
    {
        var oldPath = oldUri.AbsoluteLocation.LocalPath;
        if (node.Uri.AbsoluteLocation.LocalPath.StartsWith(oldPath))
        {
            var relativeToRenamedAncestor = node.Uri.AbsoluteLocation.LocalPath.Substring(oldPath.Length);
            var newPath = System.IO.Path.Combine(folder.AbsoluteLocation.LocalPath, relativeToRenamedAncestor);

            if (node is WorkspaceFolderViewModel)
            {
                node.Uri = new WorkspaceFolderUri(newPath, folder.WorkspaceRoot);
            }
            else if (node is WorkspaceFileViewModel)
            {
                node.Uri = new WorkspaceFileUri(newPath, folder.WorkspaceRoot);
            }

            foreach (var childNode in node.Children)
            {
                UpdateNodePath(childNode, oldUri, folder);
            }
        }
    }

    private async Task UpdateProjectFileAsync(IWorkspaceTreeNode model, WorkspaceUri oldUri)
    {
        if (!model.IsInProject)
        {
            return;
        }

        var project = _workspaceServices.ProjectFiles.Single(e => e.Uri == model.Uri.WorkspaceRoot);

        if (model is IWorkspaceFolderViewModel)
        {
            project = project with
            {
                VBProject = project.VBProject with
                {
                    Folders = project.VBProject.Folders.Except([oldUri.RelativeUriString!]).Append(model.Uri.RelativeUriString!).ToArray()
                }
            };
        }
        else if (model is IWorkspaceSourceFileViewModel)
        {
            var module = project.VBProject.Modules.Single(e => e.RelativeUri.Replace("\\", "/").Substring(1) == oldUri.RelativeUriString);
            project = project with
            {
                VBProject = project.VBProject with
                {
                    Modules = project.VBProject.Modules.Except([module]).Append(module with 
                    { 
                        // TODO also set the Name if we're also renaming the associated symbols (prompt/confirm)
                        RelativeUri = model.Uri.RelativeUriString! 
                    }).ToArray()
                }
            };
        }
        else if (model is IWorkspaceFileViewModel)
        {
            var file = project.VBProject.OtherFiles.Single(e => e.RelativeUri == oldUri.RelativeUriString);
            project = project with
            {
                VBProject = project.VBProject with
                {
                    OtherFiles = project.VBProject.OtherFiles.Except([file]).Append(file with
                    {
                        RelativeUri = model.Uri.RelativeUriString!
                    }).ToArray()
                }
            };
        }

        await _workspaceServices.UpdateProjectFileAsync(project).ConfigureAwait(false);
    }

    private void NotifyLanguageServer(Uri oldUri, Uri newUri)
    {
        var request = new DidRenameFileParams
        {
            Files = new Container<FileRename>(new WorkspaceFileRename { Uri = newUri, OldUri = oldUri })
        };
        _lsp.Value.DidRenameFile(request);
    }
}

public record class WorkspaceFileRename : FileRename
{
    public Uri OldUri { get; init; }
}