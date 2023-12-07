﻿using System.Windows;

namespace Rubberduck.UI.Splash
{
    /// <summary>
    /// Interaction logic for Splash.xaml
    /// </summary>
    public partial class SplashWindow : System.Windows.Window
    {
        public SplashWindow(ISplashViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }

        public SplashWindow()
        {
            InitializeComponent();
            MaxHeight = 380;
            MaxWidth = 340;
            InvalidateMeasure();
        }
    }
}
