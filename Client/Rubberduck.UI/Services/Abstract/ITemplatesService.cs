﻿using Rubberduck.InternalApi.Model.Workspace;
using System.Collections.Generic;

namespace Rubberduck.UI.Services.Abstract
{
    public interface ITemplatesService
    {
        void DeleteTemplate(string name);
        void SaveProjectTemplate(ProjectTemplate template);
        IEnumerable<ProjectTemplate> GetProjectTemplates();
        ProjectTemplate Resolve(ProjectTemplate template);
    }
}
