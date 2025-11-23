using Rubberduck.InternalApi.Model.Declarations.Types;
using Rubberduck.InternalApi.Model.Symbols.Abstract;
using System;

namespace Rubberduck.InternalApi.Execution.Values;

public record class VBNullValue : VBTypedValue, IVBTypedValue<VBNullValue, nint>
{
    public static VBNullValue Null { get; } = new VBNullValue();
    public VBNullValue(TypedSymbol? symbol = null) : base(VBNullType.TypeInfo, symbol) { }

    public nint Value { get; } = nint.Zero;
    public override int Size => 0;
}
