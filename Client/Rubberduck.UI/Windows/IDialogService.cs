﻿using System;

namespace Rubberduck.UI.Windows;

public interface IDialogService<TViewModel>
    where TViewModel : IDialogWindowViewModel
{
    bool ShowDialog(out TViewModel model);

    Action<TViewModel>? ConfigureModel { get; set; }
}
