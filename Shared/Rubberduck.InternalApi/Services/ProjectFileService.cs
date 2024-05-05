using Microsoft.Extensions.Logging;
using Rubberduck.InternalApi.Model.Workspace;
using Rubberduck.InternalApi.Settings;
using System;
using System.IO.Abstractions;
using System.Text.Json;
using System.Threading.Tasks;

namespace Rubberduck.InternalApi.Services;

public class ProjectFileService : ServiceBase, IProjectFileService
{
    private readonly IFileSystem _fileSystem;

    public ProjectFileService(ILogger<ProjectFileService> logger, RubberduckSettingsProvider settingsProvider,
        IFileSystem fileSystem, PerformanceRecordAggregator performance)
        : base(logger, settingsProvider, performance)
    {
        _fileSystem = fileSystem;
    }

    public async Task WriteFileAsync(ProjectFile model)
    {
        var path = _fileSystem.Path.Combine(model.Uri.LocalPath, ProjectFile.FileName);
        
        using var stream = _fileSystem.FileStream.New(path, System.IO.FileMode.OpenOrCreate);

        await JsonSerializer.SerializeAsync(stream, model);
    }

    public async Task<ProjectFile> ReadFileAsync(Uri root)
    {
        var path = _fileSystem.Path.Combine(root.LocalPath, ProjectFile.FileName);

        using var stream = _fileSystem.FileStream.New(path, System.IO.FileMode.Open);

        var project = await JsonSerializer.DeserializeAsync<ProjectFile>(stream) 
            ?? throw new InvalidOperationException($"ProjectFile could not be deserialized from '{root}'.");

        return project.WithUri(root);
    }
}
