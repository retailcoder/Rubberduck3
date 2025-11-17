using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Execution;
using Rubberduck.InternalApi.Model.Declarations.Execution.Values;
using Rubberduck.InternalApi.Model.Declarations.Types;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;

namespace Rubberduck.InternalApi.Model.Declarations.Symbols;

public record class PrecompilerConstantSymbol : ValuedTypedSymbol
{
    public PrecompilerConstantSymbol(PrecompilerConstantValue value)
        : base(RubberduckSymbolKind.Constant, value.IsFileScope ? Accessibility.Private : Accessibility.Public, value.Name, value.ParentUri, null, null)
    {
        Type = VBIntegerType.TypeInfo;
        ResolvedValueExpressionType = VBIntegerType.TypeInfo;
        Value = value;
    }

    public PrecompilerConstantValue Value { get; init; }

    public override VBTypedValue Execute(VBExecutionContext context, bool rethrow = false) => Value;
}

public record class PrecompilerConstantValue : VBIntegerValue
{
    public PrecompilerConstantValue(string name, WorkspaceUri parentUri, int value)
    {
        Name = name;
        ParentUri = parentUri;
        NumericValue = value;
    }

    public string Name { get; init; }
    public WorkspaceUri ParentUri { get; init; }

    public bool IsWorkspaceScope => ParentUri is not WorkspaceFileUri;
    public bool IsFileScope => ParentUri is WorkspaceFileUri;
}
