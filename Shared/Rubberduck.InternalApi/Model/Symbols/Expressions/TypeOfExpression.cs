using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;

namespace Rubberduck.InternalApi.Model.Symbols.Expressions;

public record class TypeOfExpression : ValuedExpression<VBTypeDescValue>
{
    public TypeOfExpression(WorkspaceUri parentUri, ValuedExpression value)
        : base(RubberduckSymbolKind.Operator, value.Type, Tokens.TypeOf, parentUri)
    {
        Expression = value;
    }

    public ValuedExpression Expression { get; }
    public override VBTypeDescValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        if (Expression.Execute(context, rethrow) is VBTypedValue result)
        {
            return new VBTypeDescValue(result.TypeInfo);
        }
        else
        {
            var exception = VBRuntimeErrorException.ObjectVariableNotSet(Expression);
            if (rethrow)
            {
                throw exception;
            }
            else
            {
                context.AddDiagnostic(RubberduckDiagnostic.RuntimeError(exception));
            }
        }

        return default;
    }
}
