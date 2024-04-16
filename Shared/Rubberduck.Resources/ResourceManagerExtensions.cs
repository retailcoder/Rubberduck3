using System.Resources;

namespace Rubberduck.Resources;

public static class ResourceManagerExtensions
{
    /// <summary>
    /// Gets the localized string for the specified key, or a "missing key" message if the key is not found.
    /// </summary>
    public static string GetLocalizedString(this ResourceManager manager, string key) =>
        manager.GetString(key) ?? $"[missing key:'{key}']";
}
