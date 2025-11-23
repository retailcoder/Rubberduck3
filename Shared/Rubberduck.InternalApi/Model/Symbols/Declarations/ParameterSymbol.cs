using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Types.Abstract;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;

namespace Rubberduck.InternalApi.Model.Symbols.Abstract;

public record class ParameterSymbol : DeclarationExpressionSymbol
{
    public ParameterSymbol(string name, WorkspaceUri parentUri, ParameterModifier modifier, VBType type)
        : base(RubberduckSymbolKind.Variable, name, parentUri, Accessibility.Private, type)
    {
        Modifier = new(modifier);
    }

    /// <summary>
    /// The zero-based position of this parameter in the parameter list.
    /// </summary>
    public int Position { get; init; }
    /// <summary>
    /// Whether this parameter is the last in the parameter list.
    /// </summary>
    public bool IsLast { get; init; }

    /// <summary>
    /// Gets information about this parameter's modifier expression.
    /// </summary>
    public ParameterSymbolModifier Modifier { get; }
}
