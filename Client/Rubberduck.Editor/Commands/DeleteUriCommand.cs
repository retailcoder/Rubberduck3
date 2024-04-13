using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using Rubberduck.InternalApi.Services;
using Rubberduck.UI.Command.Abstract;
using Rubberduck.UI.Services;
using Rubberduck.UI.Shared.Message;
using Rubberduck.UI.Shell.Tools.WorkspaceExplorer;
using System;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;
using Resx = Rubberduck.Resources.v3.RubberduckMessages;

namespace Rubberduck.Editor.Commands;

public class RemoveUriCommand : CommandBase
{
    private readonly IMessageService _messages;
    private readonly IAppWorkspacesService _workspaces;
    private readonly IProjectFileService _projectFile;

    public RemoveUriCommand(UIServiceHelper service, IMessageService messages,
        IAppWorkspacesService workspaces, IProjectFileService projectFile)
        : base(service)
    {
        _messages = messages;
        _workspaces = workspaces;
        _projectFile = projectFile;
    }

    protected override Task OnExecuteAsync(object? parameter)
    {
        if (parameter is WorkspaceUri uri)
        {
            var rdproj = _workspaces.ProjectFiles.SingleOrDefault(e => e.Uri == uri.WorkspaceRoot);
            if (rdproj != null)
            {
                if (uri is WorkspaceFileUri fileUri)
                {
                    var relativeUri = "\\" + fileUri.RelativeUriString.Replace("/", "\\");
                    var sourceFile = rdproj.VBProject.Modules.SingleOrDefault(e => e.Uri == relativeUri);
                    if (sourceFile != null)
                    {
                        rdproj.VBProject.Modules = rdproj.VBProject.Modules.Except([sourceFile]).ToArray();
                    }
                    else
                    {
                        var otherFile = rdproj.VBProject.OtherFiles.SingleOrDefault(e => e.Uri == fileUri.AbsoluteLocation.LocalPath);
                        if (otherFile != null)
                        {
                            rdproj.VBProject.OtherFiles = rdproj.VBProject.OtherFiles.Except([otherFile]).ToArray();
                        }
                    }
                }
                else if (uri is WorkspaceFolderUri folderUri)
                {
                    var folder = rdproj.VBProject.Folders.SingleOrDefault(e => e.Uri == folderUri.AbsoluteLocation.LocalPath);
                    if (folder != null)
                    {
                        rdproj.VBProject.Folders = rdproj.VBProject.Folders.Except([folder]).ToArray();
                    }

                    rdproj.VBProject.Modules = rdproj.VBProject.Modules.Where(e => !e.Uri.StartsWith(folderUri.AbsoluteLocation.LocalPath)).ToArray();
                    rdproj.VBProject.OtherFiles = rdproj.VBProject.OtherFiles.Where(e => !e.Uri.StartsWith(folderUri.AbsoluteLocation.LocalPath)).ToArray();
                    rdproj.VBProject.Folders = rdproj.VBProject.Folders.Where(e => !e.Uri.StartsWith(folderUri.AbsoluteLocation.LocalPath)).ToArray();
                }
            }
        }

        return Task.CompletedTask;
    }
}

public class DeleteUriCommand : CommandBase
{
    private readonly IMessageService _messages;
    private readonly IAppWorkspacesService _workspaces;
    private readonly IProjectFileService _projectFile;

    private readonly Lazy<IWorkspaceExplorerViewModel> _workspaceExplorer;
    private readonly Lazy<ILanguageClient> _languageClient;

    public DeleteUriCommand(UIServiceHelper service, IMessageService messages,
        IAppWorkspacesService workspaces, IProjectFileService projectFile, Func<IWorkspaceExplorerViewModel> workspaceExplorer, Func<ILanguageClient> lsp) 
        : base(service)
    {
        _messages = messages;
        _workspaces = workspaces;
        _projectFile = projectFile;
        
        _workspaceExplorer = new Lazy<IWorkspaceExplorerViewModel>(workspaceExplorer);
        _languageClient = new Lazy<ILanguageClient>(lsp);

        AddToCanExecuteEvaluation(CanExecuteWithParameter);
    }

    private bool CanExecuteWithParameter(object? parameter)
    {
        if (parameter is WorkspaceFolderUri folderUri)
        {
            return Directory.Exists(folderUri.AbsoluteLocation.LocalPath);
        }
        else if (parameter is WorkspaceFileUri fileUri)
        {
            return File.Exists(fileUri.AbsoluteLocation.LocalPath);
        }

        return false;
    }

    protected override Task OnExecuteAsync(object? parameter)
    {
        WorkspaceUri? uri;
        bool wasDeleted;

        if (parameter is WorkspaceFileUri file)
        {
            var name = file.FileName;
            uri = file;
            wasDeleted = DeleteUri(file, name);
        }
        else if (parameter is WorkspaceFolderUri folder)
        {
            var name = folder.Name;
            uri = folder;
            wasDeleted = DeleteUri(folder, name);
        }
        else
        {
            throw new NotSupportedException($"Parameter type {parameter?.GetType().Name ?? "(null)"} is not supported for this command.");
        }

        if (wasDeleted && uri is WorkspaceUri)
        {
            var workspace = _workspaces.Workspaces.GetWorkspace(uri.WorkspaceRoot);
            var workspaceFile = workspace.WorkspaceFiles.SingleOrDefault(e => e.Uri.ToString().StartsWith(uri.ToString()));

            var vm = _workspaceExplorer.Value.Workspaces.SingleOrDefault(e => e.Uri.AbsoluteLocation.LocalPath == uri.WorkspaceRoot.LocalPath);

            var rdproj = _workspaces.ProjectFiles.SingleOrDefault(e => e.Uri == uri.WorkspaceRoot);
            if (rdproj != null)
            {
                if (uri is WorkspaceFileUri fileUri)
                {
                    workspace.DeleteWorkspaceUri(fileUri);
                    var relativeUri = "\\" + fileUri.RelativeUriString.Replace("/", "\\");
                    var sourceFile = rdproj.VBProject.Modules.SingleOrDefault(e => e.Uri == relativeUri);
                    if (sourceFile != null)
                    {
                        rdproj.VBProject.Modules = rdproj.VBProject.Modules.Except([sourceFile]).ToArray();
                    }
                    else
                    {
                        var otherFile = rdproj.VBProject.OtherFiles.SingleOrDefault(e => e.Uri == fileUri.AbsoluteLocation.LocalPath);
                        if (otherFile != null)
                        {
                            rdproj.VBProject.OtherFiles = rdproj.VBProject.OtherFiles.Except([otherFile]).ToArray();
                        }
                    }

                    if (vm != null)
                    {
                        vm.RemoveWorkspaceUri(fileUri);
                    }
                }
                else if (uri is WorkspaceFolderUri folderUri)
                {
                    workspace.DeleteWorkspaceUri(folderUri);

                    var folder = rdproj.VBProject.Folders.SingleOrDefault(e => e.Uri == folderUri.AbsoluteLocation.LocalPath);
                    if (folder != null)
                    {
                        rdproj.VBProject.Folders = rdproj.VBProject.Folders.Except([folder]).ToArray();
                    }

                    rdproj.VBProject.Modules = rdproj.VBProject.Modules.Where(e => !e.Uri.StartsWith(folderUri.AbsoluteLocation.LocalPath)).ToArray();
                    rdproj.VBProject.OtherFiles = rdproj.VBProject.OtherFiles.Where(e => !e.Uri.StartsWith(folderUri.AbsoluteLocation.LocalPath)).ToArray();
                    rdproj.VBProject.Folders = rdproj.VBProject.Folders.Where(e => !e.Uri.StartsWith(folderUri.AbsoluteLocation.LocalPath)).ToArray();

                    if (vm != null)
                    {
                        vm.RemoveWorkspaceUri(folderUri);
                    }
                }

                _projectFile.WriteFile(rdproj);

                var request = new DidDeleteFileParams
                {
                    Files = new Container<FileDelete>(new FileDelete { Uri = uri })
                };
                _languageClient.Value.DidDeleteFile(request);
            }
        }

        return Task.CompletedTask;
    }

    private bool DeleteUri(WorkspaceFileUri uri, string name)
    {
        var wasDeleted = false;
        try
        {
            var model = new MessageRequestModel
            {
                Key = $"{nameof(DeleteUriCommand)}_Confirm",
                Level = LogLevel.Warning,
                Title = Resx.ResourceManager.GetString("DeleteUriCommand_ConfirmTitle") ?? "[missing key]",
                Message = string.Format(Resx.ResourceManager.GetString("DeleteUriCommand_ConfirmMessage") ?? "[missing key]", name),
                Verbose = uri.AbsoluteLocation.LocalPath,
                MessageActions = [MessageAction.AcceptConfirmAction, MessageAction.CancelAction]
            };
            if (_messages.ShowMessageRequest(model).MessageAction == MessageAction.AcceptConfirmAction)
            {
                _workspaces.FileSystem.File.Delete(uri.AbsoluteLocation.LocalPath);
                wasDeleted = true;
            }
        }
        catch (Exception exception)
        {
            Service.LogException(exception);

            // user-facing?
            _messages.ShowError("DeleteUriCommandException", exception);
        }
        return wasDeleted;
    }

    private bool DeleteUri(WorkspaceFolderUri uri, string name)
    {
        var wasDeleted = false;
        try
        {
            var model = new MessageRequestModel
            {
                Key = $"{nameof(DeleteUriCommand)}_Confirm",
                Level = LogLevel.Warning,
                Title = Resx.ResourceManager.GetString("DeleteUriCommand_ConfirmTitle") ?? "[missing key]",
                Message = string.Format(Resx.ResourceManager.GetString("DeleteUriCommand_ConfirmFolderMessage") ?? "[missing key]", name),
                Verbose = uri.AbsoluteLocation.LocalPath,
                MessageActions = [MessageAction.AcceptConfirmAction, MessageAction.CancelAction]
            };
            if (_messages.ShowMessageRequest(model).MessageAction == MessageAction.AcceptConfirmAction)
            {
                _workspaces.FileSystem.Directory.Delete(uri.AbsoluteLocation.LocalPath, true);
                wasDeleted = true;
            }
        }
        catch (Exception exception)
        {
            Service.LogException(exception);

            // user-facing?
            _messages.ShowError("DeleteUriCommandException", exception);
        }
        return wasDeleted;
    }
}