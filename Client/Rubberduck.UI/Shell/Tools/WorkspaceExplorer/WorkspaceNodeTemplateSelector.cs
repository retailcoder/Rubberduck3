using System;
using System.Windows;
using System.Windows.Controls;

namespace Rubberduck.UI.Shell.Tools.WorkspaceExplorer
{
    public class WorkspaceNodeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? WorkspaceRootTemplate { get; set; }
        public DataTemplate? FolderTemplate { get; set; }
        public DataTemplate? SourceFileTemplate { get; set; }
        public DataTemplate? WorkspaceFileTemplate { get; set; }

        public DataTemplate? GhostFolderTemplate { get; set; }
        public DataTemplate? GhostFileTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            switch (item)
            {
                case IWorkspaceSourceFileViewModel sourcefile when sourcefile.IsInProject:
                    return SourceFileTemplate ?? throw new InvalidOperationException($"{nameof(SourceFileTemplate)} is not set.");
                case IWorkspaceFileViewModel otherfile when otherfile.IsInProject:
                    return WorkspaceFileTemplate ?? throw new InvalidOperationException($"{nameof(WorkspaceFileTemplate)} is not set.");
                case IWorkspaceViewModel:
                    return WorkspaceRootTemplate ?? throw new InvalidOperationException($"{nameof(WorkspaceRootTemplate)} is not set.");
                case IWorkspaceTreeNode workfolder when workfolder.IsInProject:
                    return FolderTemplate ?? throw new InvalidOperationException($"{nameof(FolderTemplate)} is not set.");

                case IWorkspaceFileViewModel file when !file.IsInProject:
                    return GhostFileTemplate ?? throw new InvalidOperationException($"{nameof(GhostFileTemplate)} is not set.");
                case IWorkspaceTreeNode folder when !folder.IsInProject:
                    return GhostFolderTemplate ?? throw new InvalidOperationException($"{nameof(GhostFolderTemplate)} is not set.");

                default:
                    throw new NotSupportedException($"No template was found for type '{item.GetType().Name}'");
            }
        }
    }
}
