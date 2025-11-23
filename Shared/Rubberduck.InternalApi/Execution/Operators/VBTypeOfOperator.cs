using Rubberduck.InternalApi.Execution.Operators.Abstract;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model;
using Rubberduck.InternalApi.Model.Declarations.Types.Abstract;
using Rubberduck.InternalApi.Model.Symbols.Expressions;

namespace Rubberduck.InternalApi.Execution.Operators;

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
