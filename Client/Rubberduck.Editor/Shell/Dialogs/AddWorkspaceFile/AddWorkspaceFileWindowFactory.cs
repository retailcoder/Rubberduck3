using Rubberduck.UI.AddFile;
using Rubberduck.UI.Shell.AddWorkspaceFile;
using Rubberduck.UI.Windows;

namespace Rubberduck.Editor.Shell.Dialogs.AddWorkspaceFile;

public class AddWorkspaceFileWindowFactory : IWindowFactory<AddFileWindow, IAddFileWindowViewModel>
{
    public AddFileWindow Create(IAddFileWindowViewModel model) => new(model);
}
