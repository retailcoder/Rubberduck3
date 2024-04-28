using Rubberduck.InternalApi.Model.Workspace;
using Rubberduck.UI.Shell.AddWorkspaceFile;
using System.Collections.Generic;

namespace Rubberduck.UI.Services.Abstract;

public interface ITemplatesService
{
    void DeleteProjectTemplate(string name);
    void SaveProjectTemplate(ProjectTemplate template);
    IEnumerable<ProjectTemplate> GetProjectTemplates();
    ProjectTemplate Resolve(ProjectTemplate template);

    FileTemplates GetFileTemplates();
    FileTemplate Resolve(IFileTemplate template, string name);
}
