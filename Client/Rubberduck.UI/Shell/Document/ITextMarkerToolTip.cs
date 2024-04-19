using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System;
using System.Windows.Input;

namespace Rubberduck.UI.Shell.Document;

public interface ITextMarkerToolTip
{
    string Code { get; }
    string Text { get; }
    string Title { get; }
    string Type { get; }
    Uri? HelpUri { get; }

    string? SettingKey { get; }
    DiagnosticSeverity Severity { get; }

    ICommand ShowSettingsCommand { get; }
    ICommand GoToHelpUrlCommand { get; }
}
