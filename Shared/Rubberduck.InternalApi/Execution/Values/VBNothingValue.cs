using Rubberduck.InternalApi.Model.Symbols.Abstract;
using System;

namespace Rubberduck.InternalApi.Execution.Values;

public record class VBNothingValue : VBObjectValue,
    IVBTypedValue<VBObjectValue, Guid>
{
    public VBNothingValue(TypedSymbol? symbol = null) 
        : base(symbol) 
    {
        Value = Guid.Empty;
    }
}