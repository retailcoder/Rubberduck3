using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Model.Declarations.Types.Abstract;

namespace Rubberduck.InternalApi.Model.Declarations.Types;

public record class VBAnyType : VBIntrinsicType<object?>
{
    private static readonly VBAnyType _type = new();

    private VBAnyType() : base(Tokens.Any)
    {
    }

    public static VBAnyType TypeInfo => _type;

    public override bool RuntimeBinding { get; } = true;
    public override VBTypedValue DefaultValue => VBVariantType.TypeInfo.DefaultValue;
    public override VBType[] ConvertsSafelyToTypes { get; } = [];
}
