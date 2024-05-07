using Rubberduck.InternalApi.Model.Workspace;
using Rubberduck.InternalApi.Services;
using Rubberduck.InternalApi.Services.IO.Abstract;
using Rubberduck.InternalApi.Settings.Model.General;
using Rubberduck.UI.Command.Abstract;
using Rubberduck.UI.Services;
using Rubberduck.UI.Shared.NewProject;
using System;
using System.Threading.Tasks;

namespace Rubberduck.UI.Command;

public class NewProjectCommand : CommandBase
{
    private readonly INewProjectDialogService _dialog;

    private readonly IAppWorkspacesService? _workspace; // null if invoked from add-in
    private readonly IWorkspaceSyncService? _workspaceModulesService; // null if invoked from add-in

    private readonly IWorkspaceIOServices _ioServices;

    public NewProjectCommand(UIServiceHelper service,
        INewProjectDialogService dialogService,
        IAppWorkspacesService? workspace,
        IWorkspaceSyncService? workspaceModulesService,
        IWorkspaceIOServices ioServices)
        : base(service)
    {
        _dialog = dialogService;

        _workspace = workspace;
        _workspaceModulesService = workspaceModulesService;

        _ioServices = ioServices;
    }

    protected async override Task OnExecuteAsync(object? parameter)
    {
        if (_dialog.ShowDialog(out var model))
        {
            // TODO actually validate the model in the view; it's too late to fix here.

            if (Service.Settings.LanguageClientSettings.WorkspaceSettings.RequireDefaultWorkspaceRootHost)
            {
                if (model.WorkspaceLocation != Service.Settings.LanguageClientSettings.WorkspaceSettings.DefaultWorkspaceRoot.LocalPath)
                {
                    Service.LogWarning("Cannot create workspace. Project workspace location is required to be under the default workspace root as per current configuration.");
                    throw new InvalidOperationException(); // throwing here because this should have been validated already.
                }
            }

            var workspaceRootUri = _ioServices.Path.GetWorkspaceRootUri(model.WorkspaceLocation, model.ProjectName);
            var projectFile = CreateProjectFileModel(model);

            await _ioServices.WorkspaceFolder.CreateAsync(projectFile);
            await _ioServices.ProjectFile.WriteAsync(projectFile);

            if (_workspaceModulesService is not null)
            {
                // command is executed from the VBE add-in;
                // we're migrating an existing project to RD3 so we need to create the files now:
                _workspaceModulesService.ExportWorkspaceModules(workspaceRootUri, projectFile.VBProject.Modules);
            }
            else
            {
                // command is executed from the Rubberduck Editor;
                // no need to write the workspace files changes need to be saved.
            }

            if (model.SelectedProjectTemplate is not null)
            {
                var setting = Service.Settings.GeneralSettings.GetSetting<TemplatesLocationSetting>() ?? TemplatesLocationSetting.Default;
                await _ioServices.WorkspaceFolder.CreateAsync(model.SelectedProjectTemplate, setting);
            }

            Service.LogInformation("Workspace was successfully created.", $"Workspace root: {workspaceRootUri}");
            await (_workspace?.OpenProjectWorkspaceAsync(workspaceRootUri) ?? Task.CompletedTask);
        }
    }

    private ProjectFile CreateProjectFileModel(NewProjectWindowViewModel model)
    {
        var builder = new ProjectFileBuilder(_ioServices, Service.SettingsProvider);

        if (model.SelectedProjectTemplate is not null)
        {
            builder = builder.WithModel(model);
        }
        else if (model.SelectedVBProject is not null)
        {
            if (_workspaceModulesService is not null)
            {
                var workspaceRoot = new Uri(_ioServices.Path.Combine(model.WorkspaceLocation, model.ProjectName));
                var modules = _workspaceModulesService.GetWorkspaceModules(workspaceRoot, model.ScanFolderAnnotations);
                foreach (var module in modules)
                {
                    builder.WithModule(module);
                }

                var references = _workspaceModulesService.GetWorkspaceReferences(model.SelectedVBProject.WorkspaceUri);
                foreach (var reference in references)
                {
                    builder.WithReference(reference);
                }
            }
        }

        return builder.WithProjectName(model.ProjectName).Build();
    }
}
