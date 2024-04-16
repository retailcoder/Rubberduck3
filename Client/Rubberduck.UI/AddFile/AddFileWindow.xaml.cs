﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Rubberduck.UI.AddFile
{
    /// <summary>
    /// Interaction logic for AddFileWindow.xaml
    /// </summary>
    public partial class AddFileWindow : Window
    {
        public AddFileWindow()
        {
            InitializeComponent();
        }

        private void OnResizeGripDragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
        {
            var newHeight = Height + e.VerticalChange;
            var newWidth = Width + e.HorizontalChange;

            Height = Math.Min(MaxHeight, Math.Max(MinHeight, newHeight));
            Width = Math.Min(MaxWidth, Math.Max(MinWidth, newWidth));

            e.Handled = true;
        }
    }
}
