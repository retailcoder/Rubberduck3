using System;
using System.Windows;
using System.Windows.Controls;

namespace Rubberduck.UI.Shell.Tools.WorkspaceExplorer
{
    public class WorkspaceNodeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? WorkspaceNodeTemplate { get; set; }
        public DataTemplate? EditingTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            switch (item)
            {
                case IWorkspaceTreeNode node when node.IsEditingName:
                    return EditingTemplate ?? throw new InvalidOperationException($"{nameof(EditingTemplate)} is not set.");

                default:
                    return WorkspaceNodeTemplate ?? throw new InvalidOperationException($"{nameof(WorkspaceNodeTemplate)} is not set.");
            }
        }
    }
}
