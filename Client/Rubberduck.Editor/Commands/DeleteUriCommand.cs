using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using Rubberduck.Editor.Shell.Tools.WorkspaceExplorer;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Services;
using Rubberduck.UI.Command.Abstract;
using Rubberduck.UI.Services;
using Rubberduck.UI.Shared.Message;
using Rubberduck.UI.Shell.Tools.WorkspaceExplorer;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Resx = Rubberduck.Resources.v3.RubberduckMessages;

namespace Rubberduck.Editor.Commands;

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
        if (parameter is WorkspaceFolderViewModel folderVM)
        {
            return Directory.Exists(folderVM.Uri.AbsoluteLocation.LocalPath);
        }
        else if (parameter is WorkspaceFileViewModel fileVM)
        {
            return File.Exists(fileVM.Uri.AbsoluteLocation.LocalPath);
        }

        return false;
    }

    protected override Task OnExecuteAsync(object? parameter)
    {
        WorkspaceUri? uri;
        bool wasDeleted;

        if (parameter is WorkspaceFileViewModel fileVM)
        {
            var name = fileVM.FileName;
            uri = fileVM.Uri;
            wasDeleted = DeleteUri((WorkspaceFileUri)fileVM.Uri, name);
            fileVM.IsDeleted = true;
        }
        else if (parameter is WorkspaceFolderViewModel folderVM)
        {
            var name = folderVM.Name;
            uri = folderVM.Uri;
            wasDeleted = DeleteUri((WorkspaceFolderUri)uri, name);
            folderVM.IsDeleted = true;
        }
        else
        {
            throw new NotSupportedException($"Parameter type {parameter?.GetType().Name ?? "(null)"} is not supported for this command.");
        }

        if (wasDeleted && uri is WorkspaceUri)
        {
            var workspace = _workspaces.Workspaces.GetWorkspace(uri.WorkspaceRoot);
            var vm = _workspaceExplorer.Value.Workspaces.OfType<WorkspaceViewModel>().SingleOrDefault(e => e.Uri.AbsoluteLocation.LocalPath == uri.WorkspaceRoot.LocalPath);
            if (vm != null)
            {
                if (uri is WorkspaceFileUri fileUri)
                {
                    vm.RemoveWorkspaceUri(fileUri);
                }
                else if (uri is WorkspaceFolderUri folderUri)
                {
                    vm.RemoveWorkspaceUri(folderUri);
                }
                _projectFile.WriteFileAsync(vm.Model);

                NotifyLanguageServer(uri);
            }
        }

        return Task.CompletedTask;
    }

    private void NotifyLanguageServer(Uri uri)
    {
        var request = new DidDeleteFileParams
        {
            Files = new Container<FileDelete>(new FileDelete { Uri = uri })
        };
        _languageClient.Value.Workspace.DidDeleteFile(request);
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