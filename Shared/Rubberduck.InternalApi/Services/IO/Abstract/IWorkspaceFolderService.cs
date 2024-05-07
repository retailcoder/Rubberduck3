using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Workspace;
using Rubberduck.InternalApi.Settings.Model.General;
using System.Threading.Tasks;

namespace Rubberduck.InternalApi.Services.IO.Abstract;

/// <summary>
/// A service that is responsible for workspace folder I/O operations.
/// </summary>
public interface IWorkspaceFolderService
{
    /// <summary>
    /// Creates all workspace folders (empty) under the workspace source root (.src\) directory as per the provided project file.
    /// </summary>
    Task CreateAsync(ProjectFile projectFile);

    /// <summary>
    /// Copies the physical workspace files from the specified template source root to a new workspace folder for the specified project file.
    /// </summary>
    /// <remarks>
    /// Creates the necessary folders and subfolders as needed. Folders will contain a copy of the template files.
    /// </remarks>
    Task CreateAsync(ProjectTemplate template, TemplatesLocationSetting setting);

    /// <summary>
    /// Physically creates a workspace subfolder at the specified <c>Uri</c>.
    /// </summary>
    Task CreateAsync(WorkspaceFolderUri uri);

    /// <summary>
    /// Physically deletes the specified folder and all its content.
    /// </summary>
    Task DeleteAsync(WorkspaceFolderUri uri);

    /// <summary>
    /// Physically renames a folder.
    /// </summary>
    Task RenameAsync(WorkspaceFolderUri oldUri, WorkspaceFolderUri newUri);

    /// <summary>
    /// Gets the name of all files under the specified workspace folder.
    /// </summary>
    string[] GetFiles(WorkspaceFolderUri uri);
}
