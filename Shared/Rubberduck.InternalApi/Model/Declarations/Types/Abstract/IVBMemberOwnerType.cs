using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols.Abstract;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Rubberduck.InternalApi.Model.Declarations.Types.Abstract;

public interface IVBMemberOwnerType
{
    ImmutableArray<VBTypeMember> Members { get; init; }
    VBMemberOwnerType WithMembers(IEnumerable<VBTypeMember> members);
}

public abstract record class VBTypeMember
{
    /// <summary>
    /// Creates a new type member associated with a symbol.
    /// </summary>
    protected VBTypeMember(WorkspaceUri uri, string name, RubberduckSymbolKind kind, Accessibility accessibility, Symbol declaration, Symbol[]? definitions = null)
        : this(uri, name, kind, accessibility, declaration, definitions, isUserDefined: true) { }

    /// <summary>
    /// Creates a new type member without a symbol (non-user code).
    /// </summary>
    protected VBTypeMember(WorkspaceUri uri, string name, RubberduckSymbolKind kind, Accessibility accessibility, Symbol? declaration = null, Symbol[]? definitions = null, bool isUserDefined = false, bool isHidden = false)
    {
        Uri = uri;
        IsUserDefined = isUserDefined;
        Name = name;
        Kind = kind;
        Accessibility = accessibility;
        Symbol = declaration;
        Definitions = definitions ?? [];
        IsHidden = isHidden;

        DocString = string.Empty;
    }

    public WorkspaceUri Uri { get; init; }
    public bool IsUserDefined { get; init; }
    public bool IsHidden { get; init; }
    public string Name { get; init; }
    public RubberduckSymbolKind Kind { get; init; }
    public Accessibility Accessibility { get; init; }

    public string DocString { get; init; }
    public int UserMemId { get; init; }
    public int MemberFlags { get; init; }

    public Symbol? Symbol { get; init; }
    public Symbol[] Definitions { get; init; }
}
