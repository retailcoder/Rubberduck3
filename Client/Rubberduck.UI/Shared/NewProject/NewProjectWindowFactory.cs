using Rubberduck.UI.Windows;

namespace Rubberduck.UI.Shared.NewProject;

public class NewProjectWindowFactory : IWindowFactory<NewProjectWindow, NewProjectWindowViewModel>
{
    public NewProjectWindow Create(NewProjectWindowViewModel model) => new(model);
}
