using Rubberduck.UI.Command.Abstract;
using Rubberduck.UI.Services;
using Rubberduck.UI.Shell;
using Rubberduck.UI.Windows;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Rubberduck.UI.Shell.Tools.WorkspaceExplorer;

/// <summary>
/// Interaction logic for WorkspaceExplorerControl.xaml
/// </summary>
public partial class WorkspaceExplorerControl : UserControl
{
    public WorkspaceExplorerControl()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;

        CancelEditCommand = new DelegateCommand(UIServiceHelper.Instance!, parameter =>
        {
            if (parameter is IWorkspaceTreeNode node)
            {
                node.IsEditingName = false;
            }
        });
    }

    public ICommand CancelEditCommand { get; }

    private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is ITabViewModel vm)
        {
            vm.ContentControl = this;
        }

        if (e.NewValue is ICommandBindingProvider provider)
        {
            var bindings = provider.CommandBindings.ToArray();
            CommandBindings.AddRange(bindings);
            foreach (var commandBinding in bindings)
            {
                CommandManager.RegisterClassCommandBinding(typeof(WorkspaceExplorerControl), commandBinding);
            }
        }

        InvalidateVisual();
    }

    private void OnFileDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var uri = ((DataContext as IWorkspaceExplorerViewModel)?.Selection)?.Uri;
        if (uri != null)
        {
            WorkspaceExplorerCommands.OpenFileCommand.Execute(uri, this);
        }
    }

    private void FlatToggleButton_Checked(object sender, System.Windows.RoutedEventArgs e) => InvalidateVisual();

    private void OnHierarchicalDataTemplateToggled(object sender, System.Windows.RoutedEventArgs e) => InvalidateVisual();


    private bool _editOnMouseUp = false;
    private void TextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is TextBox box && box.DataContext is IWorkspaceTreeNode node)
        {
            if (node.IsSelected)
            {
                _editOnMouseUp = true;
            }
        }
    }

    private void TextBox_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is TextBox box && box.DataContext is IWorkspaceTreeNode node)
        {
            if (_editOnMouseUp && node.IsSelected)
            {
                node.IsEditingName = true;
                box.SelectAll();
            }
        }
        _editOnMouseUp = false;
    }

    private void TextBox_LostFocus(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is TextBox box && box.DataContext is IWorkspaceTreeNode node)
        {
            node.IsEditingName = false;
        }
    }

    private void StretchingTreeView_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is TreeView tree)
        {
            var point = e.GetPosition(tree);
            var node = tree.InputHitTest(point);
            if (node is DependencyObject depObj) 
            {
                var item = UIHelper.FindVisualParent<TreeViewItem>(depObj);
                if (item is TreeViewItem && item.DataContext is IWorkspaceTreeNode itemNode)
                {
                    itemNode.IsSelected = true;
                }
            }
            else
            {
                var selectedNodes = tree.ItemsSource.OfType<IWorkspaceTreeNode>().Where(e => e.IsSelected);
                foreach (var selectedNode in selectedNodes)
                {
                    selectedNode.IsSelected = false;
                }
            }
        }
    }
}
