using Rubberduck.InternalApi.Extensions;
using Rubberduck.UI.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rubberduck.UI.Shell.AddWorkspaceFile;

public interface IFileTemplate
{
    string Name { get; }
    string Description { get; }

    IEnumerable<string> Categories { get; }
    string DefaultFileName { get; }
    string FileExtension { get; }
    bool IsSourceFile { get; }
}

public interface IAddFileWindowViewModel : IDialogWindowViewModel
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
    /// <summary>
    /// The templates, filtered per the <c>Selection</c> (i.e. the selected <c>Categories</c> item).
    /// </summary>
    ICollectionView TemplatesView { get; }
    IFileTemplate SelectedTemplate { get; set; }
}
