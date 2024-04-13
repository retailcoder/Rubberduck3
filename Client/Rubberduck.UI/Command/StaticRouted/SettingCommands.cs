using System.Windows.Input;
using System.Windows.Controls;
using Resx = Rubberduck.Resources.v3.RubberduckUICommands;

namespace Rubberduck.UI.Command.StaticRouted;

public static class SettingCommands
{
    public static RoutedUICommand ShowSettingsCommand { get; }
        = new RoutedUICommand(Resx.SettingCommands_ShowSettingsCommandText, nameof(ShowSettingsCommand), typeof(UserControl));

    public static RoutedCommand AddListSettingItemCommand { get; }
        = new RoutedCommand(nameof(AddListSettingItemCommand), typeof(UserControl));
}