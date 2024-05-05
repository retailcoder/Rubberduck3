using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Workspace;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Threading.Tasks;

namespace Rubberduck.InternalApi.Services;

public class WorkspaceServiceEventArgs : EventArgs
{
    public WorkspaceServiceEventArgs(Uri uri)
    {
        Uri = uri;
    }

    public Uri Uri { get; }
}

public interface IAppWorkspacesService
{
    /// <summary>
    /// An event that is raised when a workspace/project is opened.
    /// </summary>
    event EventHandler<WorkspaceServiceEventArgs> WorkspaceOpened;
    /// <summary>
    /// An event that is raised when a workspace/project is closed.
    /// </summary>
    event EventHandler<WorkspaceServiceEventArgs> WorkspaceClosed;

    IFileSystem FileSystem { get; } // should be an implementation detail...
    IAppWorkspacesStateManager Workspaces { get; } // should be an implementation detail...

    /// <summary>
    /// Gets the project file model for all loaded workspaces.
    /// </summary>
    IEnumerable<ProjectFile> ProjectFiles { get; }
    /// <summary>
    /// Overwrites the project file with the provided model.
    /// </summary>
    /// <remarks>
    /// <c>ProjectFile.Uri</c> (assigned on load) determines the file location.
    /// </remarks>
    Task UpdateProjectFileAsync(ProjectFile projectFile);

    /// <summary>
    /// Opens the project file from the specified folder location.
    /// </summary>
    Task<bool> OpenProjectWorkspaceAsync(Uri uri);

    /// <summary>
    /// Overwrites a file in a workspace.
    /// </summary>
    /// <param name="uri">The file location relative to the workspace source root.</param>
    Task<bool> SaveWorkspaceFileAsync(WorkspaceFileUri uri);
    /// <summary>
    /// Saves a copy of a workspace file at a specified absolute location.
    /// </summary>
    /// <remarks>
    /// If the specified <c>path</c> is under the workspace source root, the copy should be added to the workspace.
    /// </remarks>
    /// <param name="uri">The original file location relative to the worspace source root.</param>
    /// <param name="path">The absolute location where the workspace file is to be copied to.</param>
    Task<bool> SaveWorkspaceFileAsAsync(WorkspaceFileUri uri, string path);
    /// <summary>
    /// Overwrites all modified files in the currently active workspace.
    /// </summary>
    Task<bool> SaveAllAsync();

    /// <summary>
    /// Closes the active workspace.
    /// </summary>
    /// <remarks>
    /// If any opened document has unsaved changes, user should be prompted (once for all unsaved documents) whether to save changes before closing.
    /// </remarks>
    /// <returns>
    /// <c>false</c> if the operation was cancelled by the user, <c>true</c> if the workspace was closed.
    /// </returns>
    Task<bool> CloseWorkspaceAsync();


}
