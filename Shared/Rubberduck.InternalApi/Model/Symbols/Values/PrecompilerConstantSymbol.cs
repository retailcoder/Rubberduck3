using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Model.Declarations.Types;
using Rubberduck.InternalApi.Model.Symbols.Abstract;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;

namespace Rubberduck.InternalApi.Model.Symbols;

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
