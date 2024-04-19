using Rubberduck.InternalApi.Model.Workspace;
using System.Collections.Generic;

namespace Rubberduck.UI.Shared.NewProject;

public interface IVBProjectInfoProvider
{
    IEnumerable<VBProjectInfo?> GetProjectInfo();
}
