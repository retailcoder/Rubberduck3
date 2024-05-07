using Rubberduck.InternalApi.Settings.Model;
using System;
using System.IO;

namespace Rubberduck.InternalApi.Settings.Model.LanguageClient;

/// <summary>
/// The default location for new projects hosted in a document that isn't saved yet.
/// </summary>
public record class DefaultWorkspaceRootSetting : UriRubberduckSetting
{
    // the default location should be a user-specific folder with read and write permissions
    private static readonly string Location = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

    public static Uri DefaultSettingValue { get; } = new(Path.Combine(Location, "Rubberduck", "Workspaces"));

    public DefaultWorkspaceRootSetting()
    {
        DefaultValue = DefaultSettingValue;
        Tags = SettingTags.Advanced | SettingTags.ReadOnlyRecommended;
    }
}
