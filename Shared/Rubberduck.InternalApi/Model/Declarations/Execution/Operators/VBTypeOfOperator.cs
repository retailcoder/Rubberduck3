using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Execution;
using Rubberduck.InternalApi.Model.Declarations.Execution.Values;
using Rubberduck.InternalApi.Model.Declarations.Operators.Abstract;
using Rubberduck.InternalApi.Model.Declarations.Symbols;
using Rubberduck.InternalApi.Model.Declarations.Types.Abstract;

namespace Rubberduck.InternalApi.Model.Declarations.Operators;

public record class VBTypeOfOperator : VBUnaryOperator
{
    public VBTypeOfOperator(ValuedExpression operand, WorkspaceUri parentUri)
        : base(Tokens.TypeOf, parentUri, operand)
    {
    }

    protected override VBTypedValue? EvaluateResult(VBExecutionContext context)
    {
        var operand = Operand.Execute(context);
        if (operand?.TypeInfo is VBType type)
        {
            return new VBTypeDescValue(type);
        }

        return default;
    }
}
