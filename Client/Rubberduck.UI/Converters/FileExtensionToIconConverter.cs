using Rubberduck.UI.Shell.AddWorkspaceFile;
using System;
using System.Globalization;
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
        if (value is IFileTemplate template)
        {
            return template.FileExtension.Replace(".", "") switch
            {
                "bas" => StandardModuleIcon,
                "cls" => ClassModuleIcon,
                "frm" => FormModuleIcon,
                // TODO add the VB6 file types here

                "txt" => TextFileIcon,
                "md" => MarkdownFileIcon,
                "json" => JsonFileIcon,

                _ => template.IsSourceFile ? DefaultSourceFileIcon : DefaultOtherFileIcon
            };
        }

        throw new InvalidOperationException();
    }
}
