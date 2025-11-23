using Rubberduck.InternalApi.Model.Declarations.Types;
using Rubberduck.InternalApi.Model.Symbols.Abstract;

namespace Rubberduck.InternalApi.Execution.Values;

public record class VBMissingValue : VBTypedValue
{
    public VBMissingValue(TypedSymbol? symbol = null)
        : base(VBMissingType.TypeInfo, symbol) { }

    public static VBMissingValue Missing { get; } = new VBMissingValue();

    public override int Size => sizeof(int);
}