using Rubberduck.UI.Windows;

namespace Rubberduck.UI.Shared.Settings;

public class SettingsWindowFactory : IWindowFactory<SettingsWindow, SettingsWindowViewModel>
{
    public SettingsWindow Create(SettingsWindowViewModel model) => new(model);
}
