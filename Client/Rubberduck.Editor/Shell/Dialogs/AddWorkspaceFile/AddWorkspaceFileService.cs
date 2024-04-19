using Microsoft.Extensions.Logging;
using Rubberduck.InternalApi.Services;
using Rubberduck.InternalApi.Settings;
using Rubberduck.InternalApi.Settings.Model;
using Rubberduck.UI.Services;
using Rubberduck.UI.Shared.Message;
using Rubberduck.UI.Shell;
using Rubberduck.UI.Shell.AddWorkspaceFile;
using Rubberduck.UI.Windows;

namespace Rubberduck.Editor.Shell.Dialogs.AddWorkspaceFile;

public interface IAddWorkspaceFileService : IDialogService<IAddFileWindowViewModel> { }

public class AddWorkspaceFileService : DialogService<AddWorkspaceFileWindow, IAddFileWindowViewModel>, IAddWorkspaceFileService
{
    private readonly UIServiceHelper _service;
    private readonly IWindowChromeViewModel _chrome;

    public AddWorkspaceFileService(UIServiceHelper service, IWindowChromeViewModel chrome,
        ILogger logger, 
        IWindowFactory<AddWorkspaceFileWindow, IAddFileWindowViewModel> factory, 
        RubberduckSettingsProvider settings, 
        MessageActionsProvider actionsProvider, 
        PerformanceRecordAggregator performance) 
        : base(logger, factory, settings, actionsProvider, performance)
    {
        _service = service;
        _chrome = chrome;
    }

    protected override IAddFileWindowViewModel CreateViewModel(RubberduckSettings settings, MessageActionsProvider actions) => 
        new AddFileWindowViewModel(_service, _chrome);
}
