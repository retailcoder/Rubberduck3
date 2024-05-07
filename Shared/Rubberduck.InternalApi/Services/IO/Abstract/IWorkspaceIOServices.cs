using System.IO.Abstractions;

namespace Rubberduck.InternalApi.Services.IO.Abstract;

/// <summary>
/// The top-level I/O service of the internal API.
/// </summary>
public interface IWorkspaceIOServices
{
    /// <summary>
    /// An abstraction for path utility methods.
    /// </summary>
    IPathService Path { get; }

    /// <summary>
    /// A service that de/serializes a Rubberduck project file (.rdproj).
    /// </summary>
    IProjectFileService ProjectFile { get; }

    /// <summary>
    /// A service that is responsible for workspace folder I/O operations.
    /// </summary>
    IWorkspaceFolderService WorkspaceFolder { get; }

    /// <summary>
    /// A service that is responsible for workspace file I/O operations.
    /// </summary>
    IWorkspaceFileService WorkspaceFile { get; }

    /// <summary>
    /// Creates a <c>FileSystemWatcher</c> for the specified root folder.
    /// </summary>
    /// <param name="root">The local workspace root directory to be watched for I/O events.</param>
    /// <remarks>
    /// The returned watcher object belongs to the caller, which is responsible for ensuring proper disposal and event de/registration.
    /// </remarks>
    IFileSystemWatcher CreateWatcherFor(string root);
}
