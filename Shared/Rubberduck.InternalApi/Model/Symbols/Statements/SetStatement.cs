using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols;
using Rubberduck.InternalApi.Model.Symbols.Abstract;
using Rubberduck.InternalApi.Model.Symbols.Expressions;

namespace Rubberduck.InternalApi.Model.Symbols.Statements;

public record class SetStatement : ExecutableStatement
{
    public SetStatement(WorkspaceUri parentUri, TypedSymbol target, ValuedExpression expression, LineLabelSymbol? parentLabel = null)
        : base(parentUri, parentLabel)
    {
        Target = target;
        Expression = expression;
    }

    public TypedSymbol Target { get; init; }
    public ValuedExpression Expression { get; init; }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        if (Expression.Execute(context, rethrow) is VBObjectValue objectValue)
        {
            if (objectValue == VBObjectValue.Nothing)
            {
                // diagnose?
            }

            context.SetSymbolValue(Target, objectValue);
        }

        return default;
    }
}
