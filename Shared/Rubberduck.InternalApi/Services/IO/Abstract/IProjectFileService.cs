using Rubberduck.InternalApi.Model.Workspace;
using Rubberduck.InternalApi.Settings.Model.General;
using System;
using System.Threading.Tasks;

namespace Rubberduck.InternalApi.Services.IO.Abstract;


/// <summary>
/// A service that de/serializes a Rubberduck project file (.rdproj).
/// </summary>
public interface IProjectFileService
{
    /// <summary>
    /// Physically creates or overwrites the project file for the specified model.
    /// </summary>
    /// <param name="model">The project file to serialize to disk.</param>
    Task WriteAsync(ProjectFile model);

    /// <summary>
    /// Deserializes a project file into a model object.
    /// </summary>
    /// <param name="root">The workspace root uri</param>
    Task<ProjectFile> ReadAsync(Uri root);
}
