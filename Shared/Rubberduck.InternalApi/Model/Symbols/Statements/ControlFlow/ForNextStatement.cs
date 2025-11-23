using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols.Expressions;
using Rubberduck.InternalApi.Model.Symbols.Statements;
using System.Collections.Generic;

namespace Rubberduck.InternalApi.Model.Symbols.Statements.ControlFlow;

public record class ForNextStatement : BlockStatement
{
    public ForNextStatement(NumericValuedExpression controlVariable, NumericValuedExpression fromExpression, NumericValuedExpression toExpression, NumericValuedExpression? stepExpression, WorkspaceUri parentUri, IEnumerable<IExecutable>? children = null)
        : base(parentUri, children)
    {
        ControlVariable = controlVariable;
        FromExpression = fromExpression;
        ToExpression = toExpression;
        StepExpression = stepExpression;
    }

    private static readonly VBNumericTypedValue ImplicitStepValue = new VBLongValue().WithValue(1);

    NumericValuedExpression ControlVariable { get; }
    NumericValuedExpression FromExpression { get; }
    NumericValuedExpression ToExpression { get; }
    NumericValuedExpression? StepExpression { get; }

    private bool Evaluate(VBExecutionContext context, bool rethrow = false)
    {
        var result = false;

        var stepValue = StepExpression?.Execute(context, rethrow) ?? ImplicitStepValue;
        var increment = ((VBNumericTypedValue)stepValue).NumericValue;

        var toValue = ToExpression.Execute(context, rethrow);
        if (toValue is VBNumericTypedValue numericToValue)
        {
            result = increment >= 0
                ? ((VBNumericTypedValue)context.CurrentScope.GetTypedValue(ControlVariable)).NumericValue <= numericToValue.NumericValue
                : ((VBNumericTypedValue)context.CurrentScope.GetTypedValue(ControlVariable)).NumericValue >= numericToValue.NumericValue;
        }

        return result;
    }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        var didEnter = false;

        var scope = context.CurrentScope;
        var initialValue = FromExpression.Execute(context, rethrow);
        if (initialValue is VBNumericTypedValue numericInitialValue)
        {
            scope.SetTypedValue(ControlVariable, numericInitialValue);
        }

        var toValue = ToExpression.Execute(context, rethrow);
        if (toValue is VBNumericTypedValue numericToValue)
        {
            while (Evaluate(context, rethrow))
            {
                didEnter = true;
                ExecuteBody(context, rethrow);

                //var currentControlValue = (scope.GetTypedValue(ControlVariable.ReferencedSymbol) as VBNumericTypedValue)!;
                //var newControlValue = currentControlValue.WithValue(currentControlValue.NumericValue + increment);

                scope.SetTypedValue(ControlVariable, toValue); // toValue instead of newControlValue to AVOID ACTUALLY LOOPING
                break; // explicit break to make the above intended behavior more obvious
            }
        }

        return new VBBooleanValue() { Value = didEnter };
    }
}
