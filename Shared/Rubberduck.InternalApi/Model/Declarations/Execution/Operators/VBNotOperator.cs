using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Execution;
using Rubberduck.InternalApi.Model.Declarations.Execution.Values;
using Rubberduck.InternalApi.Model.Declarations.Operators.Abstract;
using Rubberduck.InternalApi.Model.Declarations.Symbols;
using Rubberduck.InternalApi.Model.Declarations.Types;

namespace Rubberduck.InternalApi.Model.Declarations.Operators;

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