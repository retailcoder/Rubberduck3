using System.Windows;
using System.Windows.Controls;

namespace Rubberduck.UI.Shell;

public class StretchingTreeViewItem : TreeViewItem
{
    public StretchingTreeViewItem()
    {
        Loaded += StretchingTreeViewItem_Loaded;
    }

    private Grid _grid;
    private double _leftPadding;

    private void StretchingTreeViewItem_Loaded(object sender, RoutedEventArgs e)
    {
        if (VisualChildrenCount > 0)
        {
            if (GetVisualChild(0) is Grid grid)
            {
                grid.HorizontalAlignment = HorizontalAlignment.Stretch;
                if (grid.ColumnDefinitions.Count == 3)
                {
                    grid.ColumnDefinitions.RemoveAt(2);
                    grid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
                }

                _leftPadding = grid.ColumnDefinitions[0].ActualWidth;
                var parent = UIHelper.FindVisualParent<TreeView>(this);
                if (parent is TreeView tree)
                {
                    grid.Width = tree.ActualWidth - _leftPadding - 4;
                    tree.SizeChanged += Tree_SizeChanged;
                }

                _grid = grid;
            }
        }
    }

    private void Tree_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var tree = (TreeView)sender;
        _grid.Width = tree.ActualWidth - _leftPadding - 4;
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
