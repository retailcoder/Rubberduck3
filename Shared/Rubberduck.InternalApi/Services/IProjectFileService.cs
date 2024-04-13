using Rubberduck.InternalApi.Model.Workspace;
using System;

namespace Rubberduck.InternalApi.Services;


public interface IProjectFileService
{
    void WriteFile(ProjectFile model);
    ProjectFile ReadFile(Uri root);
}
