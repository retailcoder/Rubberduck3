using Microsoft.Extensions.Logging;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Workspace;
using Rubberduck.InternalApi.Services.IO.Abstract;
using Rubberduck.InternalApi.Settings;
using Rubberduck.InternalApi.Settings.Model.General;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;

namespace Rubberduck.InternalApi.Services.IO;

public class WorkspaceFolderService : ServiceBase, IWorkspaceFolderService
{
    private readonly IFileSystem _fs;

    public WorkspaceFolderService(ILogger<WorkspaceFolderService> logger, RubberduckSettingsProvider settingsProvider,
        IFileSystem fileSystem, PerformanceRecordAggregator performance)
        : base(logger, settingsProvider, performance)
    {
        _fs = fileSystem;
    }

    public Task CreateAsync(ProjectTemplate template, TemplatesLocationSetting setting)
    {
        var settingsRoot = _fs.DirectoryInfo.New(setting.TypedValue.LocalPath).FullName;
        var templateSrcRoot = _fs.Path.Combine(settingsRoot, template.Name, ProjectTemplate.TemplateSourceFolderName);

        var projectFile = template.ProjectFile;
        var asyncIO = new List<Task>();
        foreach (var file in projectFile.VBProject.AllFiles)
        {
            var sourcePath = _fs.Path.Combine(templateSrcRoot, file.RelativeUri);
            var destinationUri = new WorkspaceFileUri(file.RelativeUri, projectFile.Uri);

            asyncIO.Add(CreateAsync(destinationUri.WorkspaceFolder)
                .ContinueWith(t => CopyAsync(sourcePath, destinationUri.AbsoluteLocation.LocalPath), TaskScheduler.Default));
        }

        return Task.WhenAll(asyncIO);
    }

    public Task CreateAsync(ProjectFile model)
    {
        var workspaceRoot = model.Uri;
        _fs.Directory.CreateDirectory(workspaceRoot.LocalPath);

        var sourceRoot = _fs.Path.Combine(workspaceRoot.LocalPath, WorkspaceUri.SourceRootName);
        _fs.Directory.CreateDirectory(sourceRoot);

        var folders = model.VBProject.Modules
            .Select(e => new WorkspaceFileUri(e.RelativeUri, workspaceRoot).WorkspaceFolder)
            .ToHashSet()
            .Select(Folder.FromWorkspaceUri);

        model.VBProject.Folders = folders
            .Select(e => e.RelativeUri)
            .ToArray();

        var asyncIO = new List<Task>();
        foreach (var folder in folders)
        {
            var uri = folder.GetWorkspaceUri(workspaceRoot);
            asyncIO.Add(CreateAsync(uri));
        }

        return Task.WhenAll(asyncIO);
    }

    private Task CopyAsync(string sourcePath, string destinationPath) => Task.Run(() => _fs.File.Copy(sourcePath, destinationPath, overwrite: true));

    public Task CreateAsync(WorkspaceFolderUri uri) => Task.Run(() => _fs.Directory.CreateDirectory(uri.AbsoluteLocation.LocalPath));

    public Task DeleteAsync(WorkspaceFolderUri uri) => Task.Run(() => _fs.Directory.Delete(uri.AbsoluteLocation.LocalPath));

    public Task RenameAsync(WorkspaceFolderUri oldUri, WorkspaceFolderUri newUri) => Task.Run(() => _fs.Directory.Move(oldUri.AbsoluteLocation.LocalPath, newUri.AbsoluteLocation.LocalPath));

    public string[] GetFiles(WorkspaceFolderUri uri) => _fs.Directory.GetFiles(uri.AbsoluteLocation.LocalPath);
}
