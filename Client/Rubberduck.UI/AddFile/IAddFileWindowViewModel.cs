using Rubberduck.InternalApi.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rubberduck.UI.AddFile
{
    public interface IFileTemplate
    {
        string Key { get; }
        string Name { get; }
        string Description { get; }

        IEnumerable<string> Categories { get; }
        string DefaultFileName { get; }
        string FileExtension { get; }
        bool IsSourceFile { get; }
    }

    public interface IAddFileWindowViewModel
    {
        WorkspaceFolderUri ParentFolderUri { get; }

        /// <summary>
        /// The name of the file to be added, without an extension.
        /// </summary>
        string Name { get; set; }

        IEnumerable<string> Categories { get; }
        string Selection { get; set; }

        WorkspaceFileUri WorkspaceFileUri { get; }
        IEnumerable<IFileTemplate> Templates { get; }
        IFileTemplate SelectedTemplate { get; set; }
    }
}
