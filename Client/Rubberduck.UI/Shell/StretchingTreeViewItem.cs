using System.Windows;
using System.Windows.Controls;

namespace Rubberduck.UI.Shell;

public class StretchingTreeViewItem : TreeViewItem
{
    public StretchingTreeViewItem()
    {
        Loaded += StretchingTreeViewItem_Loaded;
        Expanded += StretchingTreeViewItem_Expanded;
        Collapsed += StretchingTreeViewItem_Collapsed;
    }

    private void StretchingTreeViewItem_Collapsed(object sender, RoutedEventArgs e)
    {
        _tree?.InvalidateVisual();
    }

    private void StretchingTreeViewItem_Expanded(object sender, RoutedEventArgs e)
    {
        _tree?.InvalidateVisual();
    }

    private TreeView _tree;
    private Grid _grid;

    private void StretchingTreeViewItem_Loaded(object sender, RoutedEventArgs e)
    {
        if (VisualChildrenCount > 0)
        {
            if (GetVisualChild(0) is Grid grid)
            {
                _grid = grid;

                grid.HorizontalAlignment = HorizontalAlignment.Stretch;
                if (grid.ColumnDefinitions.Count == 3)
                {
                    grid.ColumnDefinitions.RemoveAt(2);
                    grid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
                }

                var parent = UIHelper.FindVisualParent<TreeView>(this);
                if (parent is TreeView tree)
                {
                    _tree = tree;
                    OnResize(tree);
                    tree.SizeChanged += Tree_SizeChanged;
                }
            }
        }
    }

    private void Tree_SizeChanged(object sender, SizeChangedEventArgs e) => OnResize((TreeView)sender);

    private void OnResize(TreeView tree)
    {
        var leftPadding = _grid.ColumnDefinitions[0].ActualWidth + tree.Padding.Left + tree.Padding.Right;
        _grid.Width = tree.ActualWidth - leftPadding - 8;
    }

    protected override DependencyObject GetContainerForItemOverride()
    {
        return new StretchingTreeViewItem();
    }

    protected override bool IsItemItsOwnContainerOverride(object item)
    {
        return item is StretchingTreeViewItem;
    }
}
