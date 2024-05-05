using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using Rubberduck.InternalApi.Services;
using Rubberduck.UI.Command.Abstract;
using Rubberduck.UI.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Rubberduck.Editor.Commands;

public class CloseDocumentCommand : CommandBase
{
    private readonly IAppWorkspacesService _workspace;
    private readonly Func<ILanguageClient> _lsp;


    public CloseDocumentCommand(UIServiceHelper service,
        IAppWorkspacesService workspace,
        Func<ILanguageClient> lsp)
        : base(service)
    {
        _workspace = workspace;
        _lsp = lsp;
    }

    protected async override Task OnExecuteAsync(object? parameter)
    {
        if (parameter is WorkspaceFileUri uri)
        {
            if (_workspace.Workspaces.ActiveWorkspace?.TryGetWorkspaceFile(uri, out var document) ?? false)
            {
                var workspace = _workspace.Workspaces.GetWorkspace(uri.WorkspaceRoot);
                if (workspace.TryGetWorkspaceFile(uri, out var info))
                {
                    workspace.LoadDocumentState(info.WithStatus(WorkspaceFileState.Loaded));
                    // TODO update view models
                }

                var server = _lsp();
                server?.TextDocument.DidCloseTextDocument(new() { TextDocument = new() { Uri = uri.AbsoluteLocation.AbsoluteUri } });
            }
        }
    }
}
