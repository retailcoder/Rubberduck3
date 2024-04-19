﻿using Rubberduck.UI.Command.SharedHandlers;
using Rubberduck.UI.Services;
using Rubberduck.UI.Shell.Tools.WorkspaceExplorer;
using Rubberduck.UI.Windows;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;

namespace Rubberduck.UI.Shell.SaveAsTemplate;

public class SaveAsTemplateWindowViewModel : DialogWindowViewModel
{
    private readonly UIServiceHelper _service;
    private readonly IFileSystem _fileSystem;

    public SaveAsTemplateWindowViewModel(UIServiceHelper service, MessageActionCommand[] actions, IWindowChromeViewModel chrome,
        IEnumerable<IWorkspaceFileViewModel> files,
        IFileSystem fileSystem)
        : base(service, "Export Project Template", actions, chrome)
    {
        _service = service;
        _fileSystem = fileSystem;
        TemplateFiles = files;

        _name = string.Empty;
    }

    private string _name;
    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
                ValidateName();
            }
        }
    }

    private void ValidateName()
    {
        ClearErrors();

        if (string.IsNullOrWhiteSpace(Name))
        {
            AddError(nameof(Name), "A template name is required.");
            return;
        }

        var illegalChars = _fileSystem.Path.GetInvalidPathChars();
        if (illegalChars.Any(Name.Contains))
        {
            AddError(nameof(Name), $"Template names cannot contain any of the following characters: [{string.Join(',', illegalChars.Select(e => $"'{e}'"))}].");
            return;
        }
    }

    public IEnumerable<IWorkspaceFileViewModel> TemplateFiles { get; }

    protected override void ResetToDefaults() { }
}
