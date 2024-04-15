using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using Rubberduck.Editor.Shell.Tools.WorkspaceExplorer;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Workspace;
using Rubberduck.UI.Command.Abstract;
using Rubberduck.UI.Services;
using Rubberduck.UI.Shell.Tools.WorkspaceExplorer;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Rubberduck.Editor.Commands;

public class AddFileCommand : CommandBase
{
    public AddFileCommand(UIServiceHelper service) : base(service)
    {
    }

    protected override Task OnExecuteAsync(object? parameter)
    {
        if (parameter is IWorkspaceFolderViewModel parent)
        {
            
        }

        return Task.CompletedTask;
    }
}

public class AddFolderCommand : CommandBase
{
    public AddFolderCommand(UIServiceHelper service) : base(service)
    {
    }

    protected override Task OnExecuteAsync(object? parameter)
    {
        if (parameter is IWorkspaceFolderViewModel parent)
        {
            var parentPath = parent.Uri.AbsoluteLocation.LocalPath;
            var name = GetUniqueNewFolderName(parentPath);
            var path = Path.Combine(parentPath, name);
            var uri = new WorkspaceFolderUri(path, parent.Uri.WorkspaceRoot);
            var relative = uri.RelativeUriString!;

            var model = new Folder { RelativeUri = relative };
            
            var vm = WorkspaceFolderViewModel.FromModel(model, parent.Uri.WorkspaceRoot);
            parent.AddChildNode(vm);

            // ok cool, but that's just the UI.


        }

        return Task.CompletedTask;
    }

    private static readonly string DefaultNewFolderName = "NewFolder";

    private string GetUniqueNewFolderName(string parent)
    {
        var name = DefaultNewFolderName;
        var suffix = 1;

        while (Directory.Exists(Path.Combine(parent, name)))
        {
            name = DefaultNewFolderName + suffix;
            suffix++;
        }

        return name;
    }
}

public class RenameUriCommand : CommandBase
{
    private readonly Lazy<ILanguageClient> _lsp;

    public RenameUriCommand(UIServiceHelper service, Func<ILanguageClient> lsp) 
        : base(service)
    {
        _lsp = new Lazy<ILanguageClient>(lsp);
    }

    protected override Task OnExecuteAsync(object? parameter)
    {
        if (parameter is IWorkspaceFileViewModel fileNode)
        {
            var oldName = fileNode.FileName;
            var newName = fileNode.EditName;

            if (oldName != newName)
            {
                var oldUri = fileNode.Uri;
                fileNode.FileName = newName; // implementation updates the URI
                
                UpdateProjectFile();
                NotifyLanguageServer(oldUri, fileNode.Uri);
            }
        }
        else if (parameter is IWorkspaceFolderViewModel folderNode)
        {
            var oldName = folderNode.FileName;
            var newName = folderNode.EditName;

            if (oldName != newName)
            {
                var oldUri = folderNode.Uri;
                folderNode.FileName = newName;

                foreach (var childNode in folderNode.Children)
                {
                    UpdateNodePath(childNode, oldUri, (WorkspaceFolderUri)folderNode.Uri);
                }

                UpdateProjectFile();
                NotifyLanguageServer(oldUri, folderNode.Uri);
            }
        }

        return Task.CompletedTask;
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

    private void UpdateProjectFile()
    {

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