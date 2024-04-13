using Rubberduck.InternalApi.Extensions;
using Rubberduck.UI.Command.Abstract;
using Rubberduck.UI.Services;
using Rubberduck.UI.Services.Abstract;
using System;
using System.Threading.Tasks;

namespace Rubberduck.Editor.Commands;

public class OpenUriInWindowsExplorerCommand : CommandBase
{
    private readonly IUriNavigator _navigator;

    public OpenUriInWindowsExplorerCommand(UIServiceHelper service, IUriNavigator navigator) 
        : base(service)
    {
        _navigator = navigator;
    }

    protected override Task OnExecuteAsync(object? parameter)
    {
        if (parameter is Uri uri)
        {
            _navigator.Navigate(uri);
        }
        return Task.CompletedTask;
    }
}