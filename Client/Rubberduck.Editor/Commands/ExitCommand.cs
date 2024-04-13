﻿using Rubberduck.InternalApi.Services;
using Rubberduck.UI.Command.Abstract;
using Rubberduck.UI.Services;
using System.Threading.Tasks;
using System.Windows;

namespace Rubberduck.Editor.Commands;

public class ExitCommand : CommandBase
{
    private readonly IAppWorkspacesService _workspace;

    public ExitCommand(UIServiceHelper service, IAppWorkspacesService workspace)
        : base(service)
    {
        _workspace = workspace;
    }

    protected override async Task OnExecuteAsync(object? parameter)
    {
        await _workspace.SaveAllAsync();
        // TODO synchronize workspace
        Application.Current.Shutdown(); // FIXME closes the editor window, but does not exit the process...
    }
}
