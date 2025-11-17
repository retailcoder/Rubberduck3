using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Execution;
using Rubberduck.InternalApi.Model.Declarations.Execution.Values;
using Rubberduck.InternalApi.Model.Declarations.Operators.Abstract;
using Rubberduck.InternalApi.Model.Declarations.Symbols;
using System.Linq;

namespace Rubberduck.InternalApi.Model.Declarations.Operators;

public record class VBNegationOperator : VBUnaryOperator
{
    public VBNegationOperator(string expression, ValuedExpression operand, WorkspaceUri parentUri)
        : base(expression, parentUri, operand) { }

    protected override VBTypedValue? EvaluateResult(VBExecutionContext context) =>
        SymbolOperation.EvaluateUnaryOpResult(context, this, (TypedSymbol)Children!.Single(), e => -e);
}
