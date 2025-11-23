using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Types;
using Rubberduck.InternalApi.Model.Symbols.Expressions;
using Rubberduck.InternalApi.Model.Symbols.Statements;
using System.Collections.Generic;

namespace Rubberduck.InternalApi.Model.Symbols.Statements.ControlFlow;

public record class ForEachStatementSymbol : BlockStatement
{
    public ForEachStatementSymbol(ObjectValuedExpression variableExpression, ObjectValuedExpression inExpression, WorkspaceUri parentUri, IEnumerable<IExecutable>? children = null)
        : base(parentUri, children)
    {
        VariableExpression = variableExpression;
        InExpression = inExpression;
    }

    public ObjectValuedExpression VariableExpression { get; init; }
    public ObjectValuedExpression InExpression { get; init; }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        var result = false;

        if (InExpression.Execute(context, rethrow) is VBObjectValue objValue)
        {
            if (objValue.TypeInfo is VBCollectionType vbCollection && vbCollection.IsArray)
            {
                context.AddDiagnostic(RubberduckDiagnostic.EnumerationOverArray(InExpression));
            }

            result = ExecuteBody(context, rethrow);
        }

        return new VBBooleanValue() { Value = result };
    }
}
