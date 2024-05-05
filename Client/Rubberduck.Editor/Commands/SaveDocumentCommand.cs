using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Services;
using Rubberduck.UI.Command.Abstract;
using Rubberduck.UI.Services;
using System;
using System.Threading.Tasks;

namespace Rubberduck.Editor.Commands
{
    public class SaveDocumentCommand : CommandBase
    {
        private readonly IAppWorkspacesService _workspace;

        public SaveDocumentCommand(UIServiceHelper service,
            IAppWorkspacesService workspace)
            : base(service)
        {
            _workspace = workspace;
        }

        protected async override Task OnExecuteAsync(object? parameter)
        {
            if (parameter is WorkspaceFileUri uri)
            {
                await _workspace.SaveWorkspaceFileAsync(uri);
            }
        }
    }
}
