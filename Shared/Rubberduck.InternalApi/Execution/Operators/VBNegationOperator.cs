using Rubberduck.InternalApi.Execution.Operators.Abstract;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols.Abstract;
using Rubberduck.InternalApi.Model.Symbols.Expressions;
using System.Linq;

namespace Rubberduck.InternalApi.Execution.Operators;

public record class VBNegationOperator : VBUnaryOperator
{
    public VBNegationOperator(string expression, ValuedExpression operand, WorkspaceUri parentUri)
        : base(expression, parentUri, operand) { }

    protected override VBTypedValue? EvaluateResult(VBExecutionContext context) =>
        SymbolOperation.EvaluateUnaryOpResult(context, this, (TypedSymbol)Children!.Single(), e => -e);
}
