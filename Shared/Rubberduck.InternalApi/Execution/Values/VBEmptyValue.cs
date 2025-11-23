using Rubberduck.InternalApi.Model.Declarations.Types;
using Rubberduck.InternalApi.Model.Symbols.Abstract;
using System;

namespace Rubberduck.InternalApi.Execution.Values;

public record class VBEmptyValue : VBTypedValue,
    IVBTypedValue<VBEmptyValue, nint>, 
    INumericCoercion, 
    IStringCoercion
{
    public VBEmptyValue(TypedSymbol? symbol = null)
        : base(VBEmptyType.TypeInfo, symbol) { }

    public static VBEmptyValue Empty { get; } = new VBEmptyValue();

    public nint Value => nint.Zero;
    public override int Size => sizeof(int);

    public VBDoubleValue AsCoercedNumeric(int depth = 0) => VBDoubleValue.Zero;
    public VBStringValue AsCoercedString(int depth = 0) => VBStringValue.ZeroLengthString;
}
