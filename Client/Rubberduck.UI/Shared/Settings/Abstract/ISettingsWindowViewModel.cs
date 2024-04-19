namespace Rubberduck.UI.Shared.Settings.Abstract;

public interface ISettingsWindowViewModel : ICommandBindingProvider
{
    ISettingGroupViewModel Settings { get; }
    ISettingGroupViewModel Selection { get; set; }
}
