using Rubberduck.InternalApi.Model.Workspace;
using System;
using System.Threading.Tasks;

namespace Rubberduck.InternalApi.Services;


public interface IProjectFileService
{
    Task WriteFileAsync(ProjectFile model);
    Task<ProjectFile> ReadFileAsync(Uri root);
}
