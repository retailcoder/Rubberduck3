using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using Rubberduck.UI.Shell.AddWorkspaceFile;
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Media;

namespace Rubberduck.UI.Converters;

public class FileExtensionToIconConverter : ImageSourceConverter
{
    public ImageSource StandardModuleIcon { get; set; }
    public ImageSource ClassModuleIcon { get; set; }
    public ImageSource FormModuleIcon { get; set; }
    public ImageSource DefaultSourceFileIcon { get; set; }

    public ImageSource TextFileIcon { get; set; }
    public ImageSource MarkdownFileIcon { get; set; }
    public ImageSource JsonFileIcon { get; set; }
    public ImageSource DefaultOtherFileIcon { get; set; }

    public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isSourceFile = false;
        string extension = null;
        if (value is IFileTemplate template)
        {
            extension = template.FileExtension.Replace(".", "");
            isSourceFile = template.IsSourceFile;
        }
        else if (value is string ext)
        {
            extension = ext.Replace(".", "");
            isSourceFile = SupportedLanguage.VB6.FileTypes.Select(e => e.Replace("*.", "")).Contains(extension);
        }

        return extension switch
        {
            "bas" => StandardModuleIcon,
            "cls" => ClassModuleIcon,
            "frm" => FormModuleIcon,
            // TODO add the VB6 file types here

            "txt" => TextFileIcon,
            "md" => MarkdownFileIcon,
            "json" => JsonFileIcon,

            _ => isSourceFile ? DefaultSourceFileIcon : DefaultOtherFileIcon
        };
    }
}
