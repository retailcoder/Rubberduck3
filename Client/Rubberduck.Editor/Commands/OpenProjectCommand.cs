using Rubberduck.InternalApi.Model.Workspace;
using Rubberduck.InternalApi.Services;
using Rubberduck.InternalApi.Services.IO.Abstract;
using Rubberduck.InternalApi.Settings.Model.LanguageClient;
using Rubberduck.UI.Command;
using Rubberduck.UI.Command.Abstract;
using Rubberduck.UI.Command.StaticRouted;
using Rubberduck.UI.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Rubberduck.Editor.Commands
{
    public class OpenProjectCommand : CommandBase
    {
        private readonly IAppWorkspacesService _workspaceService;
        private readonly IWorkspaceIOServices _ioServices;

        public OpenProjectCommand(UIServiceHelper service,
            IAppWorkspacesService workspace, 
            IWorkspaceIOServices ioServices)
            : base(service)
        {
            _workspaceService = workspace;
            _ioServices = ioServices;
        }

        protected async override Task OnExecuteAsync(object? parameter)
        {
            string? path;
            if (parameter is null)
            {
                var setting = Service.Settings.LanguageClientSettings.WorkspaceSettings.GetSetting<DefaultWorkspaceRootSetting>();
                var workspacesRoot = setting?.TypedValue ?? DefaultWorkspaceRootSetting.DefaultSettingValue;

                var prompt = new BrowseFileModel
                {
                    Title = "Open Project",
                    DefaultFileExtension = "rdproj",
                    Filter = "Rubberduck Project (.rdproj)|*.rdproj",
                    RootUri = workspacesRoot,
                };
                if (!DialogCommands.BrowseFileOpen(prompt))
                {
                    return;
                }
                path = _ioServices.Path.GetFileName(prompt.Selection);
            }
            else
            {
                path = parameter.ToString();
            }

            if (path != null)
            {
                if (!_workspaceService.ProjectFiles.Any(project => project.Uri.LocalPath[..^(ProjectFile.FileName.Length + 1)] == path))
                {
                    Service.LogInformation("Opening project workspace...", $"Workspace root: {path}");
                    await _workspaceService.OpenProjectWorkspaceAsync(new Uri(path));
                }
            }
        }
    }
}
