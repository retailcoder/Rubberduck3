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
            var path = parameter?.ToString();
            if (string.IsNullOrWhiteSpace(path))
            {
                var setting = Service.Settings.LanguageClientSettings.WorkspaceSettings.GetSetting<DefaultWorkspaceRootSetting>();
                if (!TryBrowseWorkspaceLocation(setting, out path))
                {
                    Service.LogInformation("Operation was cancelled by the user.");
                    return;
                }
            }

            if (!TryFindWorkspaceLocation(path, out var location))
            {
                Service.LogWarning("Could not find workspace folder at the specified location.", $"Workspace root: {path}");
                return;
            }

            if (_workspaceService.IsOpened(location))
            {
                Service.LogWarning("Workspace is already opened.", $"Workspace root: {location}");
                return;
            }

            Service.LogInformation("Opening project workspace...", $"Workspace root: {location}");
            await _workspaceService.OpenProjectWorkspaceAsync(location).ConfigureAwait(false);
        }

        private bool TryBrowseWorkspaceLocation(DefaultWorkspaceRootSetting? setting, out string path)
        {
            var workspacesRoot = setting?.TypedValue ?? DefaultWorkspaceRootSetting.DefaultSettingValue;
            var model = new BrowseFileModel
            {
                Title = "Open Project",
                DefaultFileExtension = "rdproj",
                Filter = "Rubberduck Project (.rdproj)|*.rdproj",
                RootUri = workspacesRoot,
            };

            if (DialogCommands.BrowseFileOpen(model))
            {
                var fullProjectFilePath = model.Selection;
                if (fullProjectFilePath.EndsWith(ProjectFile.FileName))
                {
                    path = fullProjectFilePath[..^(ProjectFile.FileName.Length + 1)];
                    return true;
                }
                else
                {
                    Service.LogWarning($"Selection was not valid; a path to an existing {ProjectFile.FileName} file was expected.");
                }
            }

            path = default!;
            return false;
        }

        private bool TryFindWorkspaceLocation(string? path, out Uri workspaceLocation)
        {
            if (!string.IsNullOrWhiteSpace(path) && _ioServices.Path.FileExists(_ioServices.Path.Combine(path, ProjectFile.FileName)))
            {
                workspaceLocation = new Uri(path);
                return true;
            }

            workspaceLocation = default!;
            return false;
        }
    }
}
