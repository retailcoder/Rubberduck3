using System;

namespace Rubberduck.InternalApi.Services.IO.Abstract;

/// <summary>
/// An abstraction for path utility methods.
/// </summary>
public interface IPathService
{
    /// <summary>
    /// Creates a root <c>WorkspaceUri</c> for a specified location and project name.
    /// </summary>
    /// <param name="workspaceLocation">The local path to the workspace's parent folder.</param>
    /// <param name="projectName">The name of the project, which should not include characters that are illegal in a folder name.</param>
    /// <returns></returns>
    Uri GetWorkspaceRootUri(string workspaceLocation, string projectName);

    /// <summary>
    /// Gets the file name of a given <c>Uri</c>.
    /// </summary>
    string GetFileName(Uri uri);

    /// <summary>
    /// Gets the file (or folder) name from a path string.
    /// </summary>
    string GetFileName(string path);
    /// <summary>
    /// Gets the file name from a path string, without its extension.
    /// </summary>
    string GetFileNameWithoutExtension(string path);

    /// <summary>
    /// Combines path components into a path string.
    /// </summary>
    string Combine(params string[] args);

    /// <summary>
    /// Tests a path to see if a folder exists at that location.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the specified path points to an existing folder.
    /// </returns>
    bool FolderExists(string path);

    /// <summary>
    /// Tests a path to see if a file exists at that location
    /// </summary>
    /// <returns>
    /// <c>true</c> if the specified path points to an existing file.
    /// </returns>
    bool FileExists(string path);
}
