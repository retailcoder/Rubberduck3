﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Rubberduck.InternalApi.Settings.Model.Editor.Tools;

public record class ToolWindowSettings : TypedSettingGroup
{
    protected static readonly RubberduckSetting[] DefaultSettings =
        new RubberduckSetting[]
        {
            new ShowToolWindowOnStartupSetting(),
            new AutoHideToolWindowSetting(),
            new DefaultToolWindowLocationSetting(),
        };

    public ToolWindowSettings() 
    {
        Value = DefaultValue = DefaultSettings;
    }

    public ToolWindowSettings(IDictionary<string, RubberduckSetting> settings) : this()
    {
        var defaultKeys = DefaultSettings.Select(e => e.Key).ToHashSet();
        Value = settings.Where(e => defaultKeys.Contains(e.Key));
    }

    [JsonIgnore]
    public bool ShowOnStartup => GetSetting<ShowToolWindowOnStartupSetting>()?.TypedValue ?? ShowToolWindowOnStartupSetting.DefaultSettingValue;

    [JsonIgnore]
    public bool AutoHide => GetSetting<AutoHideToolWindowSetting>()?.TypedValue ?? AutoHideToolWindowSetting.DefaultSettingValue;

    [JsonIgnore]
    public DockingLocation DefaultLocation => GetSetting<DefaultToolWindowLocationSetting>()?.TypedValue ?? DefaultToolWindowLocationSetting.DefaultSettingValue;
}
