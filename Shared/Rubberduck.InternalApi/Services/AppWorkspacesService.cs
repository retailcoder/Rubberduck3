using Microsoft.Extensions.Logging;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Workspace;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using Rubberduck.InternalApi.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace Rubberduck.InternalApi.Services;

public class AppWorkspacesService : ServiceBase, IAppWorkspacesService
{
    private readonly IAppWorkspacesStateManager _workspaces;
    private readonly HashSet<ProjectFile> _projectFiles = [];

    private readonly IFileSystem _fileSystem;
    private readonly IProjectFileService _projectFileService;

    public event EventHandler<WorkspaceServiceEventArgs> WorkspaceOpened = delegate { };
    public event EventHandler<WorkspaceServiceEventArgs> WorkspaceClosed = delegate { };

    public AppWorkspacesService(ILogger<AppWorkspacesService> logger, RubberduckSettingsProvider settingsProvider,
        IAppWorkspacesStateManager workspaces, IFileSystem fileSystem, PerformanceRecordAggregator performance,
        IProjectFileService projectFileService)
        : base(logger, settingsProvider, performance)
    {
        _workspaces = workspaces;
        _fileSystem = fileSystem;
        _projectFileService = projectFileService;
    }

    public IAppWorkspacesStateManager Workspaces => _workspaces;

    public async virtual Task OnWorkspaceOpenedAsync(Uri uri) => await Task.Run(() => OnWorkspaceOpened(uri));
    protected void OnWorkspaceOpened(Uri uri) => WorkspaceOpened?.Invoke(this, new(uri));

    public void OnWorkspaceClosed(Uri uri) => WorkspaceClosed(this, new(uri));

    public IFileSystem FileSystem => _fileSystem;

    public IEnumerable<ProjectFile> ProjectFiles => _projectFiles;

    public async Task UpdateProjectFileAsync(ProjectFile projectFile) => await _projectFileService.WriteFileAsync(projectFile);

    public async Task<bool> OpenProjectWorkspaceAsync(Uri uri)
    {
        var success = await TryRunActionAsync(async () =>
            {
                if (!ValidateProjectFilePath(uri))
                {
                    throw new FileNotFoundException("No project file ('.rdproj') was found under the specified workspace URI.");
                }
                if (!ValidateProjectSourcePath(uri))
                {
                    throw new DirectoryNotFoundException("Project source root folder ('.src') was not found under the secified workspace URI.");
                }

                var projectFile = await _projectFileService.ReadFileAsync(uri);
                if (!projectFile.ValidateVersion())
                {
                    throw new NotSupportedException("This project was created with a version of Rubberduck greater than the one currently running.");
                }

                var workspace = _workspaces.AddWorkspace(uri);
                workspace.ProjectName = _fileSystem.Path.GetFileName(uri.LocalPath);

                foreach (var reference in projectFile.VBProject.References)
                {
                    workspace.AddReference(reference);
                }

                await LoadWorkspaceFilesAsync(uri, projectFile);
                _projectFiles.Add(projectFile);

            });

        await OnWorkspaceOpenedAsync(uri);
        return IsReady = IsReady || success;
    }

    private bool ValidateProjectFilePath(Uri uri)
    {
        var root = uri.LocalPath;
        var projectFilePath = _fileSystem.Path.Combine(root, ProjectFile.FileName);

        return _fileSystem.File.Exists(projectFilePath);
    }

    private bool ValidateProjectSourcePath(Uri uri)
    {
        var root = uri.LocalPath;
        var sourceRoot = _fileSystem.Path.Combine(root, WorkspaceUri.SourceRootName);

        return _fileSystem.Directory.Exists(sourceRoot);
    }

    /// <summary>
    /// <c>true</c> when there's at least one workspace opened.
    /// </summary>
    public bool IsReady { get; private set; }

    public async Task<bool> SaveWorkspaceFileAsync(WorkspaceFileUri uri)
    {
        var workspace = _workspaces.ActiveWorkspace;
        if (workspace?.WorkspaceRoot != null && workspace.TryGetWorkspaceFile(uri, out var file) && file != null)
        {
            var path = _fileSystem.Path.Combine(workspace.WorkspaceRoot.LocalPath, WorkspaceUri.SourceRootName, file.Uri.LocalPath);
            await _fileSystem.File.WriteAllTextAsync(path, file.Text);

            workspace.SaveWorkspaceFile(uri);
            return true;
        }

        return false;
    }

    public async Task<bool> SaveWorkspaceFileAsAsync(WorkspaceFileUri uri, string path)
    {
        var workspace = _workspaces.ActiveWorkspace;
        if (workspace?.WorkspaceRoot != null && workspace.TryGetWorkspaceFile(uri, out var file) && file != null)
        {
            // note: saves a copy but only keeps the original URI in the workspace
            await _fileSystem.File.WriteAllTextAsync(path, file.Text);
            return true;
        }

        return false;
    }

    public async Task<bool> SaveAllAsync()
    {
        var tasks = new List<Task>();
        var workspace = _workspaces.ActiveWorkspace;
        if (workspace?.WorkspaceRoot != null)
        {
            var srcRoot = _fileSystem.Path.Combine(workspace.WorkspaceRoot.AbsoluteLocation.LocalPath, WorkspaceUri.SourceRootName);
            foreach (var file in workspace.WorkspaceFiles.Where(e => e.IsModified).ToArray())
            {
                var path = _fileSystem.Path.Combine(srcRoot, file.Uri.ToString());
                tasks.Add(_fileSystem.File.WriteAllTextAsync(path, file.Text));

                if (!workspace.SaveWorkspaceFile(file.Uri))
                {
                    LogWarning("Could not reset document version.", file.Uri.ToString());
                }
            }
        }

        return await Task.WhenAll(tasks).ContinueWith(t => !t.IsFaulted, TaskScheduler.Current);
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
        var (status, content, documentVersion) = await GetWorkspaceFileStateAsync(uri, open);

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

        if (!_fileSystem.File.Exists(path))
        {
            LogWarning($"File '{uri}' does not exist in this workspace and will have no content and status 'Missing'.");
            return (WorkspaceFileState.Missing, string.Empty, -1);
        }

        try
        { 
            var content = await _fileSystem.File.ReadAllTextAsync(path).ConfigureAwait(false);

            var status = open 
                ? WorkspaceFileState.Opened 
                : WorkspaceFileState.Loaded;

            LogInformation($"File '{uri}' was loaded successfully. Status is '{status}'.");
            return (status, content, 1);
        }
        catch (Exception exception)
        {
            LogException(exception);
            LogWarning($"File '{uri}' could not be loaded and will have no content and status 'LoadError'.");
            return (WorkspaceFileState.LoadError, string.Empty, -1);
        }
    }

    public Task<bool> CloseWorkspaceAsync()
    {
        var uri = _workspaces.ActiveWorkspace?.WorkspaceRoot ?? throw new InvalidOperationException("WorkspaceStateManager.WorkspaceRoot is unexpectedly null.");
        _workspaces.Unload(uri);

        OnWorkspaceClosed(uri);
        return Task.FromResult(true);
    }
}
