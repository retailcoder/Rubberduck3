using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Windows.Input;

namespace Rubberduck.UI.Shell.Document;

internal class TextMarkerToolTipViewModel : ITextMarkerToolTip
{
    public string Code { get; init; }
    public string Title { get; init; }
    public string Type { get; init; }
    public string Text { get; init; }
    public string? SettingKey { get; init; }
    public DiagnosticSeverity Severity { get; init; }
    public Uri? HelpUri { get; init; }

    public ICommand ShowSettingsCommand { get; init; }
    public ICommand GoToHelpUrlCommand { get; init; }
}