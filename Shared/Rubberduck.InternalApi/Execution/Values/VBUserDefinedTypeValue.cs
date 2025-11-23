using Rubberduck.InternalApi.Model.Declarations.Types;
using Rubberduck.InternalApi.Model.Declarations.Types.Abstract;
using Rubberduck.InternalApi.Model.Symbols.Abstract;
using System;
using System.Linq;

namespace Rubberduck.InternalApi.Execution.Values;

public record class VBUserDefinedTypeValue : VBTypedValue, 
    IVBTypedValue<VBUserDefinedTypeValue, Guid>
{
    public VBUserDefinedTypeValue(VBUserDefinedType type, TypedSymbol? symbol = null) 
        : base(type, symbol)
    {
        Value = Guid.NewGuid();
    }

    public Guid Value { get; }

    public override int Size => ((IVBMemberOwnerType)TypeInfo).Members.OfType<VBUserDefinedTypeMember>()
        .Sum(member => ((TypedSymbol)member.Symbol!).Type!.DefaultValue.Size);
}
