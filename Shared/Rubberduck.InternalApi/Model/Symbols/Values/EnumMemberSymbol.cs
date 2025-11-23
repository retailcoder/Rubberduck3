using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Types;
using Rubberduck.InternalApi.Model.Symbols.Abstract;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;

namespace Rubberduck.InternalApi.Model.Symbols.Values;

public record class EnumMemberSymbol : ValuedTypedSymbol
{
    public EnumMemberSymbol(string name, WorkspaceUri parentUri, string? value)
        : base(RubberduckSymbolKind.EnumMember, Accessibility.Public, name, parentUri, null, value)
    {
        Type = VBLongType.TypeInfo;
    }

    public override VBTypedValue Execute(VBExecutionContext context, bool rethrow = false) => context.CurrentScope.GetTypedValue(this);
}
