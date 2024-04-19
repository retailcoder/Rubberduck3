﻿using System;

namespace Rubberduck.UI.Services.Abstract;

public interface IUriNavigator
{
    /// <summary>
    /// Opens the specified URI in the default browser.
    /// </summary>
    void Navigate(Uri uri);
}
