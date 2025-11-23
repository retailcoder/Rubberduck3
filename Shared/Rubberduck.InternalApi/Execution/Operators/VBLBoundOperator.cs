using Rubberduck.InternalApi.Execution.Operators.Abstract;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model;
using Rubberduck.InternalApi.Model.Symbols.Expressions;

namespace Rubberduck.InternalApi.Execution.Operators;

public record class VBLBoundOperator : VBOperator
{
    public VBLBoundOperator(WorkspaceUri parentUri, ValuedExpression arrayOperand, ValuedExpression? dimensionIndex = null)
        : base(Tokens.LBound, parentUri, dimensionIndex is null ? [arrayOperand] : [arrayOperand, dimensionIndex!])
    {
        ArrayOperand = arrayOperand;
        DimensionIndex = dimensionIndex;
    }

    public ValuedExpression ArrayOperand { get; init; }

    public ValuedExpression? DimensionIndex { get; init; }

    protected override VBTypedValue? EvaluateResult(VBExecutionContext context)
    {
        var array = ArrayOperand.Execute(context) as VBArrayValue;
        var dimension = DimensionIndex?.Execute(context) as VBNumericTypedValue;

        if (array != null)
        {
            var index = dimension?.AsLong().Value ?? 0;
            return new VBLongValue(ArrayOperand) { NumericValue = array.Dimensions[index].LowerBound };
        }

        throw VBCompileErrorException.ExpectedArray(ArrayOperand!, "Use the `LBound` operator to find the lower boundary of an array variable.");
    }
}
