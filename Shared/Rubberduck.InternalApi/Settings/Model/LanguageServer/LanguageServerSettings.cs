﻿using Rubberduck.InternalApi.Settings.Model.LanguageServer.Diagnostics;
using Rubberduck.InternalApi.Settings.Model.ServerStartup;
using System.Text.Json.Serialization;

namespace Rubberduck.InternalApi.Settings.Model.LanguageServer;

/// <summary>
/// Configures LSP (Language Server Protocol) server options.
/// </summary>
public record class LanguageServerSettings : TypedSettingGroup, IDefaultSettingsProvider<LanguageServerSettings>
{
    private static readonly RubberduckSetting[] DefaultSettings =
        [
            new TraceLevelSetting { Value = TraceLevelSetting.DefaultSettingValue },
            new LanguageServerStartupSettings { Value = LanguageServerStartupSettings.DefaultSettings },
            new DiagnosticsSettings { Value =  DiagnosticsSettings.DefaultSettings },
        ];

    public LanguageServerSettings()
    {
        DefaultValue = DefaultSettings;
    }

    [JsonIgnore]
    public MessageTraceLevel TraceLevel => GetSetting<TraceLevelSetting>()?.TypedValue ?? TraceLevelSetting.DefaultSettingValue;
    [JsonIgnore]
    public LanguageServerStartupSettings StartupSettings => GetSetting<LanguageServerStartupSettings>() ?? LanguageServerStartupSettings.Default;
    [JsonIgnore]
    public DiagnosticsSettings Diagnostics => GetSetting<DiagnosticsSettings>() ?? DiagnosticsSettings.Default;

    public static LanguageServerSettings Default { get; } = new() { Value = DefaultSettings, DefaultValue = DefaultSettings };
    LanguageServerSettings IDefaultSettingsProvider<LanguageServerSettings>.Default => Default;
}
