using Rubberduck.InternalApi.Services.IO.Abstract;
using System.IO.Abstractions;

namespace Rubberduck.InternalApi.Services.IO;

public class WorkspaceIOServices : IWorkspaceIOServices
{
    private readonly IFileSystem _fs;

    public WorkspaceIOServices(IFileSystem fs,
        IPathService path,
        IProjectFileService projectFile,
        IWorkspaceFolderService workspaceFolder,
        IWorkspaceFileService workspaceFile)
    {
        _fs = fs;

        Path = path;
        ProjectFile = projectFile;
        WorkspaceFolder = workspaceFolder;
        WorkspaceFile = workspaceFile;
    }

    public IPathService Path { get; }
    public IProjectFileService ProjectFile { get; }
    public IWorkspaceFolderService WorkspaceFolder { get; }
    public IWorkspaceFileService WorkspaceFile { get; }

    public IFileSystemWatcher CreateWatcherFor(string root) => _fs.FileSystemWatcher.New(root);
}
