using Rubberduck.UI.Shell.AddWorkspaceFile;
using Rubberduck.UI.Windows;

namespace Rubberduck.Editor.Shell.Dialogs.AddWorkspaceFile;

public class AddWorkspaceFileWindowFactory : IWindowFactory<AddWorkspaceFileWindow, IAddFileWindowViewModel>
{
    public AddWorkspaceFileWindow Create(IAddFileWindowViewModel model) => new(model);
}
