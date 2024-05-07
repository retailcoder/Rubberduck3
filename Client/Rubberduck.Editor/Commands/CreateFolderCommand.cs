using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using Rubberduck.Editor.Services;
using Rubberduck.Editor.Shell.Tools.WorkspaceExplorer;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Workspace;
using Rubberduck.InternalApi.Services;
using Rubberduck.InternalApi.Services.IO.Abstract;
using Rubberduck.UI.Command.Abstract;
using Rubberduck.UI.Services;
using Rubberduck.UI.Shared.Message;
using Rubberduck.UI.Shell.AddWorkspaceFile;
using Rubberduck.UI.Shell.Tools.WorkspaceExplorer;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Rubberduck.Editor.Commands;

public class CreateFolderCommand : CommandBase
{
    private static readonly string DefaultNewFolderName = "NewFolder";
    private readonly IFileSystemServices _fileSystemServices;
    private readonly IProjectFileService _projectFileService;
    private readonly IAppWorkspacesService _workspaceService;
    private readonly IMessageService _messages;
    private readonly Lazy<ILanguageClient> _lsp;

    private readonly IWorkspaceIOServices _ioServices;

    public CreateFolderCommand(UIServiceHelper service, IMessageService messages, IWorkspaceIOServices ioServices, IFileSystemServices fileSystemServices,
        IProjectFileService projectFileService, IAppWorkspacesService workspaceService, Func<ILanguageClient> lsp) 
        : base(service)
    {
        _ioServices = ioServices;
        _fileSystemServices = fileSystemServices;
        _projectFileService = projectFileService;

        _messages = messages;
        _workspaceService = workspaceService;


        _lsp = new Lazy<ILanguageClient>(lsp);
    }

    protected override async Task OnExecuteAsync(object? parameter)
    {
        if (parameter is IWorkspaceFolderViewModel parent)
        {
            WorkspaceFolderUri? uri = default;
            Folder? model = default;

            if (Service.TryRunAction(() =>
            {
                var parentPath = parent.Uri.AbsoluteLocation.LocalPath;
                var name = GetUniqueNewFolderName(parentPath);
                var path = Path.Combine(parentPath, name);
                uri = new WorkspaceFolderUri(path, parent.Uri.WorkspaceRoot);

                var relative = uri.RelativeUriString!;
                model = new Folder { RelativeUri = relative };

            }, $"{nameof(CreateFolderCommand)} (UI)") && uri != default)
            {
                if (Service.TryRunAction(() => _fileSystemServices.CreateFolder(uri), out var exception, $"{nameof(CreateFolderCommand)} (IO)"))
                {
                    await UpdateProjectFileAsync(uri);
                    NotifyLanguageServer(uri);

                    if (model != default)
                    {
                        var vm = WorkspaceFolderViewModel.FromModel(model, parent.Uri.WorkspaceRoot);
                        parent.AddChildNode(vm);
                    }
                }
                else if (exception != null)
                {
                    _messages.ShowError($"{nameof(CreateFolderCommand)}_Error", exception);
                }
            }
        }
    }

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

    private async Task UpdateProjectFileAsync(WorkspaceFolderUri uri)
    {
        var project = _workspaceService.ProjectFiles.Single(e => e.Uri == uri.WorkspaceRoot);

        project = project with
        {
            VBProject = project.VBProject with
            {
                Folders = project.VBProject.Folders.Append(uri.RelativeUriString!).ToArray()
            }
        };
        
        await Task.WhenAll(_projectFileService.WriteAsync(project), _workspaceService.UpdateProjectFileAsync(project));
    }

    private void NotifyLanguageServer(Uri uri) 
    {
        // LSP "file" == "folder"
        var request = new DidCreateFileParams
        {
            Files = new Container<FileCreate>(new FileCreate { Uri = uri }),
        };
        _lsp.Value.Workspace.DidCreateFile(request);
    }
}
