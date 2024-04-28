using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using Rubberduck.Editor.Services;
using Rubberduck.InternalApi.Model.Workspace;
using Rubberduck.InternalApi.Services;
using Rubberduck.InternalApi.Settings;
using Rubberduck.InternalApi.Settings.Model;
using Rubberduck.UI.Services;
using Rubberduck.UI.Services.Abstract;
using Rubberduck.UI.Shared.Message;
using Rubberduck.UI.Shell;
using Rubberduck.UI.Shell.AddWorkspaceFile;
using Rubberduck.UI.Windows;
using System;
using System.Linq;

namespace Rubberduck.Editor.Shell.Dialogs.AddWorkspaceFile;

public interface IAddWorkspaceFileService : IDialogService<IAddFileWindowViewModel> { }

public class AddWorkspaceFileService : DialogService<AddWorkspaceFileWindow, IAddFileWindowViewModel>, IAddWorkspaceFileService
{
    private readonly UIServiceHelper _service;
    private readonly IWindowChromeViewModel _chrome;
    private readonly ITemplatesService _templates;
    private readonly IFileSystemServices _fileSystemServices;
    private readonly IProjectFileService _projectFileService;
    private readonly IAppWorkspacesService _workspaceServices;
    private readonly Lazy<ILanguageClient> _lsp;

    public AddWorkspaceFileService(UIServiceHelper service, IWindowChromeViewModel chrome,
        ILogger<AddWorkspaceFileService> logger,
        IWindowFactory<AddWorkspaceFileWindow, IAddFileWindowViewModel> factory, ITemplatesService templates, IFileSystemServices fileSystemServices,
        IProjectFileService projectFileService, IAppWorkspacesService workspaceService,
        RubberduckSettingsProvider settings, 
        MessageActionsProvider actionsProvider, 
        PerformanceRecordAggregator performance,
        Func<ILanguageClient> lsp) 
        : base(logger, factory, settings, actionsProvider, performance)
    {
        _service = service;
        _chrome = chrome;
        _templates = templates;
        _fileSystemServices = fileSystemServices;
        _projectFileService = projectFileService;
        _workspaceServices = workspaceService;

        _lsp = new Lazy<ILanguageClient>(lsp);
    }

    protected override IAddFileWindowViewModel CreateViewModel(RubberduckSettings settings, MessageActionsProvider actions) => 
        new AddFileWindowViewModel(_service, _chrome, actions.OkCancel(), _templates);

    protected override void OnDialogAccept(IAddFileWindowViewModel model)
    {
        var resolved = _templates.Resolve(model.SelectedTemplate, model.Name);
        _fileSystemServices.CreateFile(model.WorkspaceFileUri.AbsoluteLocation, resolved.TextContent);
        
        UpdateProjectFile(model);
        NotifyLanguageServer(model.WorkspaceFileUri);
    }

    private void UpdateProjectFile(IAddFileWindowViewModel model)
    {
        var project = _workspaceServices.ProjectFiles.Single(e => e.Uri == model.WorkspaceFileUri.WorkspaceRoot);

        if (model.SelectedTemplate.IsSourceFile)
        {
            var module = new Module
            {
                Name = model.Name,
                RelativeUri = model.WorkspaceFileUri.RelativeUriString!,
            };
            project = project with 
            { 
                VBProject = project.VBProject with 
                { 
                    Modules = project.VBProject.Modules.Append(module).ToArray()
                }
            };
        }
        else
        {
            var file = new File
            {
                RelativeUri = model.WorkspaceFileUri.RelativeUriString!,
            };
            project = project with 
            {
                VBProject = project.VBProject with 
                {
                    OtherFiles = project.VBProject.OtherFiles.Append(file).ToArray()
                }
            };
        }

        _projectFileService.WriteFile(project);
        _workspaceServices.UpdateProjectFile(project);
    }

    private void NotifyLanguageServer(Uri uri)
    {
        var request = new DidCreateFileParams
        {
            Files = new Container<FileCreate>(new FileCreate { Uri = uri }),
        };
        _lsp.Value.Workspace.DidCreateFile(request);
    }
}
