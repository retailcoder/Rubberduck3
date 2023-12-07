﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Rubberduck.UI.Shell
{
    /// <summary>
    /// Interaction logic for FlatButton.xaml
    /// </summary>
    public partial class FlatToggleButton : ToggleButton
    {
        public FlatToggleButton()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Checked += OnCheckedChanged;
            Unchecked += OnCheckedChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            OnCheckedChanged(sender, new());
        }

        private void OnCheckedChanged(object sender, RoutedEventArgs e)
        {
            IconSource = IsChecked == true ? CheckedIcon : Icon;
        }

        public ImageSource? IconSource 
        {
            get => (ImageSource?)GetValue(IconSourceProperty);
            set
            {
                SetProperty(IconSourceProperty, value);
            }
        }
        public static readonly DependencyProperty IconSourceProperty =
            DependencyProperty.Register(nameof(IconSource), typeof(ImageSource), typeof(FlatToggleButton));

        public ImageSource? Icon 
        {
            get => (ImageSource?)GetValue(IconProperty);
            set
            {
                SetProperty(IconProperty, value);
            }
        }
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(ImageSource), typeof(FlatToggleButton));

        public ImageSource? CheckedIcon 
        { 
            get => (ImageSource?)GetValue(CheckedIconProperty);
            set => SetProperty(CheckedIconProperty, value);
        }
        public static readonly DependencyProperty CheckedIconProperty =
            DependencyProperty.Register(nameof(CheckedIcon), typeof(ImageSource), typeof(FlatToggleButton));

        private void SetProperty<TValue>(DependencyProperty property, TValue value)
        {
            var oldValue = (TValue)GetValue(property);
            var newValue = value;
            if (newValue is object && !newValue.Equals(oldValue))
            {
                SetValue(property, newValue);
                OnPropertyChanged(new(property, oldValue, newValue));
            }
        }
    }
}
