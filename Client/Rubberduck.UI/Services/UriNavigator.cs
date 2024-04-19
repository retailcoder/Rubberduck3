using Rubberduck.InternalApi.Extensions;
using Rubberduck.UI.Services.Abstract;
using System;
using System.Diagnostics;

namespace Rubberduck.UI.Services;

public class UriNavigator : IUriNavigator
{
    // TODO validate that we can get a 200/OK from the specified URL before actually launching a browser process.

    public void Navigate(Uri uri)
    {
        try
        {
            if (uri is WorkspaceFolderUri folder)
            {
                var path = folder.AbsoluteLocation.LocalPath;
                var info = new ProcessStartInfo(path)
                {
                    WorkingDirectory = path,
                    UseShellExecute = true,
                };
                Process.Start(info);
            }
            else if (uri.IsAbsoluteUri)
            {
                Process.Start(new ProcessStartInfo(uri.AbsoluteUri));
            }
        }
        catch
        {
            // gulp.
        }
    }
}
