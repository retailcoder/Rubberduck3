using System;

namespace Rubberduck.InternalApi.Model.Workspace;

public record class Reference
{
    public static Reference VBStandardLibrary { get; } = new Reference
    {
        Guid = new Guid("000204ef-0000-0000-c000-000000000046"),
        Uri = "C:\\Program Files\\Microsoft Office\\root\\vfs\\ProgramFilesCommonX64\\Microsoft Shared\\VBA\\VBA7.1\\VBE7.DLL",
        Name = "VBA",
        Major = 4,
        Minor = 2,
        IsUnremovable = true,
    };

    public string Name { get; set; }
    /// <summary>
    /// The absolute path to the referenced project or library.
    /// </summary>
    public string? Uri { get; set; }
    public Guid? Guid { get; set; }
    public int? Major { get; set; }
    public int? Minor { get; set; }
    public string? TypeLibInfoUri { get; set; }

    public bool IsUnremovable { get; set; }
}
