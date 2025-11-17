using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Execution;
using Rubberduck.InternalApi.Model.Declarations.Execution.Values;
using Rubberduck.InternalApi.Model.Declarations.Operators.Abstract;
using Rubberduck.InternalApi.Model.Declarations.Symbols;
using System;
using System.Linq;

namespace Rubberduck.InternalApi.Model.Declarations.Operators;

public enum AssignmentKind
{
    ValueAssignment,
    ReferenceAssignment
}

public record class VBAssignmentOperator : VBBinaryOperator
{
    private readonly AssignmentKind _kind;

    public VBAssignmentOperator(AssignmentKind kind, WorkspaceUri parentUri, ValuedExpression lhs, ValuedExpression rhs)
        : base(Tokens.CompareEqualOp, parentUri, lhs, rhs)
    {
        _kind = kind;
    }

    public override VBTypedValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        return base.Execute(context, rethrow);
    }

    protected override VBTypedValue ExecuteBinaryOperator(VBExecutionContext context, VBTypedValue lhsValue, VBTypedValue rhsValue)
    {
        var assignmentTarget = lhsValue;

        if (lhsValue is VBObjectValue lhsObject)
        {
            if (_kind == AssignmentKind.ReferenceAssignment && assignmentTarget.TypeInfo != rhsValue.TypeInfo)
            {
                if (assignmentTarget.TypeInfo.ConvertsSafelyToTypes.Contains(rhsValue.TypeInfo))
                {
                    context.AddDiagnostic(RubberduckDiagnostic.TypeCastConversion(this));
                }
                else
                {
                    throw VBRuntimeErrorException.TypeMismatch(Range, "This reference assignment is referring to incompatible object types.");
                }
            }
            else if (_kind == AssignmentKind.ValueAssignment)
            {
                assignmentTarget = lhsObject.LetCoerce();
                context.AddDiagnostic(RubberduckDiagnostic.ImplicitLetCoercion(lhsObject.Symbol!));

                if (rhsValue is VBObjectValue rhsObject)
                {
                    context.AddDiagnostic(RubberduckDiagnostic.SuspiciousValueAssignment(this));
                }
            }
        }

        if (assignmentTarget.TypeInfo != rhsValue.TypeInfo)
        {
            if (!assignmentTarget.TypeInfo.ConvertsSafelyToTypes.Contains(rhsValue.TypeInfo))
            {
                if (rhsValue is VBObjectValue rhsObject && _kind == AssignmentKind.ValueAssignment)
                {
                    var letCoercedValue = rhsObject.LetCoerce();
                    context.AddDiagnostic(RubberduckDiagnostic.ImplicitLetCoercion(rhsObject.Symbol!));
                    context.CurrentScope.SetTypedValue(lhsValue.Symbol!, letCoercedValue);
                    return letCoercedValue;
                }

                throw VBRuntimeErrorException.TypeMismatch(Range, "This value assignment is referring to incompatible data types.");
            }
            else if (_kind == AssignmentKind.ValueAssignment)
            {
                if (assignmentTarget.TypeInfo.ConvertsSafelyToTypes.Contains(rhsValue.TypeInfo))
                {
                    context.AddDiagnostic(RubberduckDiagnostic.ImplicitWideningConversion(this));
                }
                else
                {
                    throw VBRuntimeErrorException.TypeMismatch(Range, "This value assignment is referring to conflicting data types.");
                }
            }
        }

        context.CurrentScope.SetTypedValue(assignmentTarget.Symbol!, rhsValue);
        return rhsValue;
    }
}
