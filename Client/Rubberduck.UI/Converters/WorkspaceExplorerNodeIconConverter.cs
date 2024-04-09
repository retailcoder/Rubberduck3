using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Rubberduck.UI.Shell.Tools.WorkspaceExplorer;

namespace Rubberduck.UI.Converters;

public class WorkspaceExplorerNodeIconConverter : ImageSourceConverter, IMultiValueConverter
{
    public ImageSource WorkspaceIcon { get; set; }
    public ImageSource WorkspaceFolderClosedIcon { get; set; }
    public ImageSource WorkspaceFolderOpenIcon { get; set; }
    public ImageSource WorkspaceSourceFileIcon { get; set; }
    public ImageSource WorkspaceOtherFileIcon { get; set; }
    public ImageSource GhostFolderClosedIcon { get; set; }
    public ImageSource GhostFolderOpenIcon { get; set; }
    public ImageSource GhostFileIcon { get; set; }


    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        switch (value)
        {
            case IWorkspaceSourceFileViewModel sourceFile:
                return sourceFile.IsInProject
                    ? WorkspaceSourceFileIcon
                    : GhostFileIcon;
            case IWorkspaceFileViewModel file:
                return file.IsInProject
                    ? WorkspaceOtherFileIcon
                    : GhostFileIcon;
            case IWorkspaceFolderViewModel folder:
                return folder.IsInProject
                    ? folder.IsExpanded
                        ? WorkspaceFolderOpenIcon
                        : WorkspaceFolderClosedIcon
                    : folder.IsExpanded
                        ? GhostFolderOpenIcon
                        : GhostFolderClosedIcon;
            default:
                return null!;
        }
    }


    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
        Convert(values[0], targetType, parameter, culture);

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}