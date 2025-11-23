using Rubberduck.InternalApi.Execution.Operators.Abstract;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model;
using Rubberduck.InternalApi.Model.Declarations.Types;
using Rubberduck.InternalApi.Model.Symbols.Expressions;

namespace Rubberduck.InternalApi.Execution.Operators;

public record class VBNotOperator : VBUnaryOperator
{
    public VBNotOperator(WorkspaceUri parentUri, ValuedExpression operand)
        : base(Tokens.Not, parentUri, operand) { }

    protected override VBTypedValue? EvaluateResult(VBExecutionContext context)
    {
        var operand = Operand.Execute(context);
        if (operand?.TypeInfo != VBBooleanType.TypeInfo)
        {
            context.AddDiagnostic(RubberduckDiagnostic.BitwiseOperator(this));
        }
        return SymbolOperation.EvaluateUnaryOpResult(context, this, Operand, e => ~(int)e);
    }
}