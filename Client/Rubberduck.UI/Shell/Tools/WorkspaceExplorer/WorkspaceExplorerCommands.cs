using System.Windows;
using System.Windows.Input;
using Resx = Rubberduck.Resources.v3.RubberduckUICommands;

namespace Rubberduck.UI.Shell.Tools.WorkspaceExplorer;

public static class WorkspaceExplorerCommands
{
    public static RoutedUICommand OpenFileCommand { get; }
        = new RoutedUICommand(Resx.WorkspaceExplorerCommands_OpenFileCommandText, nameof(OpenFileCommand), typeof(WorkspaceExplorerControl));

    public static RoutedUICommand CancelRenameUriCommand { get; }
        = new RoutedUICommand(Resx.WorkspaceExplorerCommands_CancelRenameUriCommandText, nameof(CancelRenameUriCommand), typeof(WorkspaceExplorerControl));
    public static RoutedUICommand RenameUriCommand { get; }
        = new RoutedUICommand(Resx.WorkspaceExplorerCommands_RenameUriCommandText, nameof(RenameUriCommand), typeof(WorkspaceExplorerControl));

    public static RoutedUICommand DeleteUriCommand { get; }
        = new RoutedUICommand(Resx.WorkspaceExplorerCommands_DeleteUriCommandText, nameof(DeleteUriCommand), typeof(WorkspaceExplorerControl));

    public static RoutedUICommand CreateFileCommand { get; }
        = new RoutedUICommand(Resx.WorkspaceExplorerCommands_CreateFileCommandText, nameof(CreateFileCommand), typeof(WorkspaceExplorerControl));

    public static RoutedUICommand CreateFolderCommand { get; }
        = new RoutedUICommand(Resx.WorkspaceExplorerCommands_CreateFolderCommandText, nameof(CreateFolderCommand), typeof(WorkspaceExplorerControl));

    public static RoutedUICommand IncludeFileCommand { get; }
        = new RoutedUICommand(Resx.WorkspaceExplorerCommands_IncludeFileCommandText, nameof(IncludeFileCommand), typeof(WorkspaceExplorerControl));
    public static RoutedUICommand ExcludeFileCommand { get; }
        = new RoutedUICommand(Resx.WorkspaceExplorerCommands_ExcludeFileCommandText, nameof(ExcludeFileCommand), typeof(WorkspaceExplorerControl));

    public static RoutedUICommand ExpandFolderCommand { get; }
        = new RoutedUICommand(Resx.WorkspaceExplorerCommands_ExpandFolderCommandText, nameof(ExpandFolderCommand), typeof(WorkspaceExplorerControl));
    public static RoutedUICommand CollapseFolderCommand { get; }
        = new RoutedUICommand(Resx.WorkspaceExplorerCommands_CollapseFolderCommandText, nameof(CollapseFolderCommand), typeof(WorkspaceExplorerControl));


    public static RoutedUICommand NewProjectCommand { get; }
        = new RoutedUICommand(Resx.FileCommands_NewProjectCommandText, nameof(NewProjectCommand), typeof(Window));
    public static RoutedUICommand OpenProjectWorkspaceCommand { get; }
        = new RoutedUICommand(Resx.FileCommands_OpenProjectCommandText, nameof(OpenProjectWorkspaceCommand), typeof(Window));

    public static RoutedUICommand CloseProjectWorkspaceCommand { get; }
        = new RoutedUICommand(Resx.FileCommands_CloseProjectCommandText, nameof(CloseProjectWorkspaceCommand), typeof(Window));
    public static RoutedUICommand RenameProjectWorkspaceCommand { get; }
        = new RoutedUICommand(Resx.FileCommands_RenameProjectCommandText, nameof(RenameProjectWorkspaceCommand), typeof(Window));

    public static RoutedUICommand SynchronizeProjectWorkspaceCommand { get; }
        = new RoutedUICommand(Resx.FileCommands_SynchronizeProjectCommandText, nameof(SynchronizeProjectWorkspaceCommand), typeof(Window));

    public static RoutedUICommand OpenFolderInWindowsExplorerCommand { get; }
        = new RoutedUICommand(Resx.FileCommands_OpenFolderInWindowsExplorerCommandText, nameof(OpenFolderInWindowsExplorerCommand), typeof(Window));
}
