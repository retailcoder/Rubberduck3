using AsyncAwaitBestPractices;
using Microsoft.Extensions.Logging;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Workspace;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using Rubberduck.InternalApi.Services.IO.Abstract;
using Rubberduck.InternalApi.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Rubberduck.InternalApi.Services;

public class AppWorkspacesService : ServiceBase, IAppWorkspacesService
{
    private readonly IAppWorkspacesStateManager _workspaces;
    private readonly IWorkspaceIOServices _ioServices;

    private readonly Dictionary<Uri, ProjectFile> _projectFiles = [];

    public event EventHandler<WorkspaceServiceEventArgs> WorkspaceOpened = delegate { };
    public event EventHandler<WorkspaceServiceEventArgs> WorkspaceClosed = delegate { };

    public AppWorkspacesService(ILogger<AppWorkspacesService> logger, RubberduckSettingsProvider settingsProvider, PerformanceRecordAggregator performance,
        IWorkspaceIOServices ioServices,IAppWorkspacesStateManager workspaces)
        : base(logger, settingsProvider, performance)
    {
        _workspaces = workspaces;
        _ioServices = ioServices;
    }

    protected IWorkspaceIOServices IOServices => _ioServices;
    public IAppWorkspacesStateManager Workspaces => _workspaces;

    public bool IsOpened(Uri root) => _projectFiles.Keys.Contains(root);

    /// <summary>
    /// For a LSP client, starts the LSP server at the specified workspace URI.
    /// </summary>
    /// <remarks>
    /// For a LSP server, raises <c>WorkspaceOpened</c>, which should trigger processing.
    /// </remarks>
    public async virtual Task OnWorkspaceOpenedAsync(Uri uri) => await Task.Run(() => OnWorkspaceOpened(uri));
    /// <summary>
    /// Raises the <c>WorkspaceOpened</c> event.
    /// </summary>
    protected void OnWorkspaceOpened(Uri uri) => WorkspaceOpened?.Invoke(this, new(uri));

    /// <summary>
    /// Raises the <c>WorkspaceClosed</c> event.
    /// </summary>
    public void OnWorkspaceClosed(Uri uri) => WorkspaceClosed(this, new(uri));

    public IEnumerable<ProjectFile> ProjectFiles => _projectFiles.Values;

    public async Task UpdateProjectFileAsync(ProjectFile projectFile)
    {
        _projectFiles[projectFile.Uri] = projectFile;
        await _ioServices.ProjectFile.WriteAsync(projectFile).ConfigureAwait(false);
    }

    public async Task<bool> OpenProjectWorkspaceAsync(Uri uri)
    {
        try
        {
            if (!_ioServices.Path.FileExists(_ioServices.Path.Combine(uri.LocalPath, ProjectFile.FileName)))
            {
                throw new FileNotFoundException("No project file ('.rdproj') was found under the specified workspace URI.");
            }
            if (!_ioServices.Path.FolderExists(_ioServices.Path.Combine(uri.LocalPath, WorkspaceUri.SourceRootName)))
            {
                throw new DirectoryNotFoundException("Project source root folder ('.src') was not found under the secified workspace URI.");
            }

            var projectFile = await _ioServices.ProjectFile.ReadAsync(uri).ConfigureAwait(false);
            if (!projectFile.ValidateVersion())
            {
                throw new NotSupportedException("This project was created with a version of Rubberduck greater than the one currently running.");
            }

            var workspace = _workspaces.AddWorkspace(uri);
            workspace.ProjectName = _ioServices.Path.GetFileName(uri);

            foreach (var reference in projectFile.VBProject.References)
            {
                workspace.AddReference(reference);
            }

            await LoadWorkspaceFilesAsync(uri, projectFile).ConfigureAwait(false);
            _projectFiles[projectFile.Uri] = projectFile;

            OnWorkspaceOpenedAsync(uri).SafeFireAndForget();
            return true;
        }
        catch (Exception exception)
        {
            LogException(exception);

            // discard: we only care that this uri isn't a key when we exit; whether it was or wasn't added/removed is irrelevant.
            _ = _projectFiles.Remove(uri);

            return false;
        }
    }

    /// <summary>
    /// <c>true</c> when there's at least one workspace opened.
    /// </summary>
    public bool IsReady { get; private set; }

    public async Task SaveWorkspaceFileAsync(WorkspaceFileUri uri)
    {
        var workspace = _workspaces.ActiveWorkspace;
        if (workspace?.WorkspaceRoot != null && workspace.TryGetWorkspaceFile(uri, out var file) && file != null)
        {
            await _ioServices.WorkspaceFile.WriteAsync(file).ConfigureAwait(false);
            workspace.ResetDocumentVersion(uri);
        }
    }

    public Task SaveWorkspaceFileAsAsync(WorkspaceFileUri uri, string path)
    {
        var workspace = _workspaces.ActiveWorkspace;
        if (workspace?.WorkspaceRoot != null && workspace.TryGetWorkspaceFile(uri, out var file) && file != null)
        {
            // note: saves a copy but only keeps the original URI in the workspace
            return _ioServices.WorkspaceFile.SaveCopyAsync(file, new Uri(path));
        }

        return Task.CompletedTask;
    }

    public Task SaveAllAsync()
    {
        var asyncIO = new List<Task>();
        var workspace = _workspaces.ActiveWorkspace;
        if (workspace?.WorkspaceRoot != null)
        {
            var srcRoot = _ioServices.Path.Combine(workspace.WorkspaceRoot.AbsoluteLocation.LocalPath, WorkspaceUri.SourceRootName);
            foreach (var file in workspace.WorkspaceFiles.Where(e => e.IsModified).ToArray())
            {
                asyncIO.Add(_ioServices.WorkspaceFile.WriteAsync(file));
                if (!workspace.ResetDocumentVersion(file.Uri))
                {
                    LogWarning("Could not reset document version.", $"{file.Uri} (v{file.Version})");
                }
            }
        }

        return Task.WhenAll(asyncIO);
    }

    private async Task LoadWorkspaceFilesAsync(Uri workspaceRoot, ProjectFile projectFile)
    {
        var workspace = _workspaces.ActiveWorkspace;
        if (workspace is null)
        {
            LogWarning("There is currently no active workspace to load workspace files into.");
            throw new InvalidOperationException("No active workspace; cannot load workspace files.");
        }

        var asyncIO = new List<Task>();
        foreach (var file in projectFile.VBProject.Modules)
        {
            asyncIO.Add(LoadWorkspaceFileAsync(workspace, file.GetWorkspaceUri(workspaceRoot), isSourceFile: true, projectFile.VBProject.ProjectType, file.IsAutoOpen));
        }
        foreach (var file in projectFile.VBProject.OtherFiles)
        {
            asyncIO.Add(LoadWorkspaceFileAsync(workspace, file.GetWorkspaceUri(workspaceRoot), isSourceFile: false, projectFile.VBProject.ProjectType, file.IsAutoOpen));
        }

        await Task.WhenAll(asyncIO).ConfigureAwait(false);
    }

    private async Task LoadWorkspaceFileAsync(IWorkspaceState workspace, WorkspaceFileUri uri, bool isSourceFile, ProjectType projectType, bool open = false)
    {
        var (status, content, documentVersion) = await GetWorkspaceFileStateAsync(uri, open).ConfigureAwait(false);

        DocumentState info;
        if (isSourceFile)
        {
            // project source files are VB source code, for now. with more language servers this could change.
            var language = projectType == ProjectType.VBA ? SupportedLanguage.VBA : SupportedLanguage.VB6;
            info = new CodeDocumentState(uri, language, content, status, documentVersion);
        }
        else
        {
            info = new DocumentState(uri, content, status, documentVersion);
        }

        if (workspace.LoadDocumentState(info))
        {
            LogInformation($"{(isSourceFile ? "Source" : "File")} document '{uri}' was loaded successfully.", $"IsOpened: {info.Status == WorkspaceFileState.Opened}");
        }
        else
        {
            LogWarning($"{(isSourceFile ? "Source file" : "File")} document '{uri}' version {documentVersion} was not loaded; a newer version is already cached.'.");
        }
    }

    private async Task<(WorkspaceFileState status, string content, int documentVersion)> GetWorkspaceFileStateAsync(WorkspaceFileUri uri, bool open = false)
    {
        var path = uri.AbsoluteLocation.LocalPath;

        if (!_ioServices.Path.FileExists(path))
        {
            LogWarning($"File '{uri}' does not exist in this workspace and will have no content and status 'Missing'.");
            return (WorkspaceFileState.Missing, string.Empty, DocumentState.InvalidVersion);
        }

        try
        {
            var content = await _ioServices.WorkspaceFile.ReadAsync(uri).ConfigureAwait(false);

            var status = open 
                ? WorkspaceFileState.Opened 
                : WorkspaceFileState.Loaded;

            LogInformation($"File '{uri}' was loaded successfully. Status is '{status}'.");
            return (status, content, DocumentState.InitialVersion);
        }
        catch (Exception exception)
        {
            LogException(exception);
            LogWarning($"File '{uri}' could not be loaded and will have no content and status 'LoadError'.");
            return (WorkspaceFileState.LoadError, string.Empty, DocumentState.InvalidVersion);
        }
    }

    public Task<bool> CloseWorkspaceAsync()
    {
        var uri = _workspaces.ActiveWorkspace?.WorkspaceRoot 
            ?? throw new InvalidOperationException("WorkspaceStateManager.WorkspaceRoot is unexpectedly null.");

        _workspaces.Unload(uri);

        OnWorkspaceClosed(uri);
        return Task.FromResult(true);
    }
}
