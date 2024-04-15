namespace Rubberduck.InternalApi.Model.Workspace;

public record class Module : File
{
    /// <summary>
    /// The name of the module; must be unique across the entire workspace.
    /// </summary>
    /// <remarks>
    /// The value of the module's <c>VB_Name</c> attribute.
    /// </remarks>
    public string Name { get; set; }

    /// <summary>
    /// Identifies the base class (supertype) for specific types of supported document modules, if applicable.
    /// </summary>
    public DocClassType? Super { get; set; }
}
