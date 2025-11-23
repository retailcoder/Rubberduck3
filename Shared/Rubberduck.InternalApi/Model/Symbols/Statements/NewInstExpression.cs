using Rubberduck.InternalApi.Execution;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Symbols.Declarations;
using Rubberduck.InternalApi.Model.Symbols.Expressions.Operators;
using System;

namespace Rubberduck.InternalApi.Model.Symbols.Statements;

/// <summary>
/// Represents a <c>New</c> expression, that creates a new instance of a class.
/// </summary>
public record class NewInstExpression : OperatorExpression<VBObjectValue>
{
    public NewInstExpression(WorkspaceUri parentUri, ClassModuleSymbol classType)
        : base(Tokens.New, classType.Type!, parentUri, [])
    {
        ClassType = classType;
    }

    public ClassModuleSymbol ClassType { get; }

    public override VBObjectValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        return new VBObjectValue(ClassType) { Value = Guid.NewGuid() };
    }
}
