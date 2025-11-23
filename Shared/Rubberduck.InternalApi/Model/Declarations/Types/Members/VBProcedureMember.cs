using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Types.Abstract;
using Rubberduck.InternalApi.Model.Symbols.Abstract;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;

namespace Rubberduck.InternalApi.Model.Declarations.Types;

/// <summary>
/// Describes a <c>VBTypeMember</c> that can be executed with an execution context.
/// </summary>
public abstract record class VBExecutableMember : VBTypeMember
{
    public VBExecutableMember(WorkspaceUri uri, string name, RubberduckSymbolKind kind, Accessibility accessibility, Symbol? declaration, TypedSymbol[]? definitions = null, bool isUserDefined = false)
        : base(uri, name, kind, accessibility, declaration, definitions, isUserDefined)
    {
    }

    public bool? IsReachable { get; init; }

    public virtual VBTypedValue? Execute(VBExecutionContext context) => ((IExecutable)Symbol!).Execute(context);
}

public record class VBProcedureMember : VBExecutableMember
{
    public VBProcedureMember(WorkspaceUri uri, string name, RubberduckSymbolKind kind, Accessibility accessibility, Symbol? declaration, TypedSymbol[]? definitions = null, bool isUserDefined = false)
        : base(uri, name, kind, accessibility, declaration, definitions, isUserDefined)
    {
    }
}
