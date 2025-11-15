using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Execution;
using Rubberduck.InternalApi.Model.Declarations.Execution.Values;
using Rubberduck.InternalApi.Model.Declarations.Operators;
using Rubberduck.InternalApi.Model.Declarations.Types;
using Rubberduck.InternalApi.Model.Declarations.Types.Abstract;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using System;
using System.Collections.Generic;
using System.Linq;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Rubberduck.InternalApi.Model.Declarations.Symbols;

public interface IValuedExpression<TValue> where TValue : VBTypedValue
{
    /// <summary>
    /// Evaluates the expression in the given scope.
    /// </summary>
    /// <returns>Returns a <c>VBTypedValue</c> representing the result of the expression.</returns>
    TValue? Evaluate(ref VBExecutionScope scope, bool rethrow = false);
}

public interface IExecutable : IValuedExpression<VBTypedValue>
{
    /// <summary>
    /// Executes the symbol and its children, in the given context.
    /// </summary>
    /// <returns>
    /// Returns a <c>VBTypedValue</c> representing the result of the expression; <c>null</c> if the symbol is a non-returning executable member.
    /// </returns>
    VBTypedValue? Execute(ref VBExecutionContext context, bool rethrow = false);
}

public abstract record class ValuedExpression : TypedSymbol, IValuedExpression<VBTypedValue>
{
    protected ValuedExpression(RubberduckSymbolKind kind, string name, WorkspaceUri parentUri, IEnumerable<ValuedExpression>? children = null)
        : base(kind, Accessibility.Undefined, name, parentUri, children)
    {
    }

    public virtual VBTypedValue? Evaluate(ref VBExecutionScope scope, bool rethrow = false) => Children?.OfType<ValuedExpression>().FirstOrDefault()?.Evaluate(ref scope, rethrow);
}

public record class TypeDescValuedExpression : ValuedExpression<VBTypeDescValue>
{
    public TypeDescValuedExpression(WorkspaceUri parentUri, VBType type)
        : base(RubberduckSymbolKind.Class, VBTypeDesc.TypeInfo, type.Name, parentUri)
    {
        ReferredType = type;
    }
    public VBType ReferredType { get; }
    public override VBTypeDescValue? Evaluate(ref VBExecutionScope scope, bool rethrow = false)
    {
        return new VBTypeDescValue(ReferredType);
    }
}

public abstract record class ValuedExpression<TValue> : ValuedExpression
    where TValue : VBTypedValue
{
    protected ValuedExpression(RubberduckSymbolKind kind, VBType vbType, string name, WorkspaceUri parentUri, IEnumerable<ValuedExpression>? children = null)
        : base(kind, name, parentUri, children)
    {
        Type = vbType;
    }

    public VBType Type { get; }
    public override TValue? Evaluate(ref VBExecutionScope scope, bool rethrow = false)
    {
        return Children?.OfType<ValuedExpression<TValue>>().FirstOrDefault()?.Evaluate(ref scope, rethrow) as TValue;
    }
}

public abstract record class OperatorExpression<TValue> : ValuedExpression<TValue>
    where TValue : VBTypedValue
{
    protected OperatorExpression(string name, VBType vbType, WorkspaceUri parentUri, IEnumerable<ValuedExpression>? children = null)
        : base(RubberduckSymbolKind.Operator, vbType, name, parentUri, children)
    {
    }
}

public abstract record class OperatorExpression : OperatorExpression<VBTypedValue>
{
    protected OperatorExpression(string name, WorkspaceUri parentUri, IEnumerable<ValuedExpression>? children = null)
        : this(name, VBVariantType.TypeInfo, parentUri, children)
    {
    }

    protected OperatorExpression(string name, VBType vbType, WorkspaceUri parentUri, IEnumerable<ValuedExpression>? children = null)
        : base(name, vbType, parentUri, children)
    {
    }
}
public abstract record class BooleanOperatorExpression : OperatorExpression<VBBooleanValue>
{
    protected BooleanOperatorExpression(string name, WorkspaceUri parentUri, ValuedExpression lhs, ValuedExpression rhs)
        : base(name, VBBooleanType.TypeInfo, parentUri, [lhs, rhs])
    {
        Left = lhs;
        Right = rhs;
    }

    public ValuedExpression Left { get; }
    public ValuedExpression Right { get; }
}

public abstract record class BitwiseOpExpression : BooleanOperatorExpression
{
    public BitwiseOpExpression(WorkspaceUri parentUri, string op, ValuedExpression left, ValuedExpression right)
        : base(op, parentUri, left, right)
    {
    }

    protected abstract Func<int, int, int> BitwiseOp { get; }

    public override VBBooleanValue? Evaluate(ref VBExecutionScope scope, bool rethrow = false)
    {
        var lhs = Left.Evaluate(ref scope, rethrow);
        var intLhs = 0;
        if (lhs is VBBooleanValue vbBoolL)
        {
            intLhs = vbBoolL.Value ? -1 : 0;
        }
        else if (lhs is VBNumericTypedValue vbNumL)
        {
            intLhs = (int)vbNumL.NumericValue;
        }

        var rhs = Right.Evaluate(ref scope, rethrow);
        var intRhs = 0;
        if (rhs is VBBooleanValue vbBoolR)
        {
            intRhs = vbBoolR.Value ? -1 : 0;
        }
        else if (rhs is VBNumericTypedValue vbNumR)
        {
            intRhs = (int)vbNumR.NumericValue;
        }

        return BitwiseOp(intLhs, intRhs) == 0
            ? VBBooleanValue.False
            : VBBooleanValue.True;
    }
}

public record class LogicalAndOpExpression : BitwiseOpExpression
{
    public LogicalAndOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(parentUri, Tokens.LogicalAndOp, left, right)
    {
    }

    protected override Func<int, int, int> BitwiseOp => (lhs, rhs) => lhs & rhs;
}

public record class LogicalOrOpExpression : BitwiseOpExpression
{
    public LogicalOrOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(parentUri, Tokens.LogicalOrOp, left, right)
    {
    }

    protected override Func<int, int, int> BitwiseOp => (lhs, rhs) => lhs | rhs;
}

public record class LogicalXOrOpExpression : BitwiseOpExpression
{
    public LogicalXOrOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(parentUri, Tokens.LogicalXOrOp, left, right)
    {
    }

    protected override Func<int, int, int> BitwiseOp => (lhs, rhs) => lhs ^ rhs;
}

public record class LogicalEqvOpExpression : BitwiseOpExpression
{
    public LogicalEqvOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(parentUri, Tokens.LogicalEqvOp, left, right)
    {
    }
    protected override Func<int, int, int> BitwiseOp => (lhs, rhs) => ~(lhs ^ rhs);
}

public record class LogicalImpOpExpression : BitwiseOpExpression
{
    public LogicalImpOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(parentUri, Tokens.LogicalImpOp, left, right)
    {
    }
    protected override Func<int, int, int> BitwiseOp => (lhs, rhs) => ~lhs | rhs;
}


public abstract record class CompareOpExpression : BooleanOperatorExpression
{
    public CompareOpExpression(WorkspaceUri parentUri, string op, ValuedExpression left, ValuedExpression right)
        : base(op, parentUri, left, right)
    {
    }

    protected abstract bool CompareOp(double left, double right);

    public override VBBooleanValue? Evaluate(ref VBExecutionScope scope, bool rethrow = false)
    {
        VBBooleanValue? result = default;

        var lhsValue = Left.Evaluate(ref scope, rethrow);
        if (lhsValue is VBNumericTypedValue lhsNumeric)
        {
            var rhsValue = Right.Evaluate(ref scope, rethrow);
            if (rhsValue is VBNumericTypedValue rhsNumeric)
            {
                result = SymbolOperation.EvaluateCompareOpResult(ref scope, this, lhsNumeric, rhsNumeric, CompareOp);
            }
            else if (rhsValue?.TypeInfo.ConvertsSafelyToType(lhsValue.TypeInfo) ?? false)
            {
                // TODO coerced numeric
            }
        }

        // TODO string lhs/coerced string rhs

        return result;
    }
}

public record class EqCompareOpExpression : CompareOpExpression
{
    public EqCompareOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(parentUri, Tokens.CompareEqualOp, left, right)
    {
    }

    protected override bool CompareOp(double left, double right) => left.Equals(right); // TODO .VBEquals(right)
}
public record class NeqCompareOpExpression : CompareOpExpression
{
    public NeqCompareOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(parentUri, Tokens.CompareNotEqualOp, left, right)
    {
    }

    protected override bool CompareOp(double left, double right) => !left.Equals(right); // TODO !.VBEquals(right)
}
public record class LtCompareOpExpression : CompareOpExpression
{
    public LtCompareOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(parentUri, Tokens.CompareLessThanOp, left, right)
    {
    }

    protected override bool CompareOp(double left, double right) => Comparer<double>.Default.Compare(left, right) < 0;
}
public record class LEqCompareOpExpression : CompareOpExpression
{
    public LEqCompareOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(parentUri, Tokens.CompareLessThanOrEqualOp, left, right)
    {
    }

    protected override bool CompareOp(double left, double right) => Comparer<double>.Default.Compare(left, right) <= 0;
}
public record class GtCompareOpExpression : CompareOpExpression
{
    public GtCompareOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(parentUri, Tokens.CompareGreaterThanOp, left, right)
    {
    }

    protected override bool CompareOp(double left, double right) => Comparer<double>.Default.Compare(left, right) > 0;
}
public record class GEqCompareOpExpression : CompareOpExpression
{
    public GEqCompareOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(parentUri, Tokens.CompareGreaterThanOrEqualOp, left, right)
    {
    }

    protected override bool CompareOp(double left, double right) => Comparer<double>.Default.Compare(left, right) >= 0;
}

public record class LikeCompareOpExpression : CompareOpExpression
{
    public LikeCompareOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(parentUri, Tokens.CompareLikeOp, left, right)
    {
    }

    protected override bool CompareOp(double left, double right) => throw new NotImplementedException();
}

public record class IsCompareOpExpression : CompareOpExpression
{
    public IsCompareOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(parentUri, Tokens.CompareIsOp, left, right)
    {
    }

    protected override bool CompareOp(double left, double right) => throw new NotImplementedException();
}

public record class ConcatOpExpression : OperatorExpression<VBStringValue>
{
    public ConcatOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(Tokens.ConcatOp, VBStringType.TypeInfo, parentUri, [left, right])
    {
        Left = left;
        Right = right;
    }
    public ValuedExpression Left { get; }
    public ValuedExpression Right { get; }

    public override VBStringValue? Evaluate(ref VBExecutionScope scope, bool rethrow = false)
    {
        var rhs = Right.Evaluate(ref scope, rethrow) as VBStringValue;
        var lhs = Left.Evaluate(ref scope, rethrow) as VBStringValue;

        // TODO handle adding VBRuntimeErrorException.TypeMismatch to the execution context

        var result = $"{lhs?.Value}{rhs?.Value}";
        return new VBStringValue().WithValue(result);
    }
}

public record class ParenthesizedExpression : OperatorExpression
{
    public ParenthesizedExpression(WorkspaceUri parentUri, ValuedExpression innerExpression)
        : base("()", innerExpression.ResolvedType!, parentUri, [innerExpression])
    {
        InnerExpression = innerExpression;
    }
    public ValuedExpression InnerExpression { get; }
    public override VBTypedValue? Evaluate(ref VBExecutionScope scope, bool rethrow = false)
    {
        return InnerExpression.Evaluate(ref scope, rethrow);
    }
}

public record class AddOpExpression : OperatorExpression
{
    public AddOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(Tokens.AdditionOp, parentUri, [left, right])
    {
        Left = left;
        Right = right;
    }

    public ValuedExpression Left { get; }
    public ValuedExpression Right { get; }

    public override VBTypedValue? Evaluate(ref VBExecutionScope scope, bool rethrow = false)
    {
        VBTypedValue? result = default;

        var lhsValue = Left.Evaluate(ref scope, rethrow);
        if (lhsValue is VBStringValue lhsString)
        {
            // if LHS coerces to a string, RHS is coerced to string and concatenated
            var rhsValue = Right.Evaluate(ref scope, rethrow);
            if (rhsValue?.TypeInfo.ConvertsSafelyToType(VBStringType.TypeInfo) ?? false)
            {
                // TODO safely convert value to string

                if (rhsValue is VBStringValue rhsString)
                {
                    var resultValue = $"{lhsString.Value}{rhsString.Value}";
                    result = new VBStringValue().WithValue(resultValue);
                }
            }
            else
            {
                // if we cannot coerce RHS to string, VBA would throw a type mismatch at runtime.
                // TODO handle adding VBRuntimeErrorException.TypeMismatch to the execution context
                //scope = scope.WithError(VBRuntimeErrorException.TypeMismatch(this));
                result = new VBErrorValue().WithValue(VBRuntimeErrorException.TypeMismatch(default(Range)!).VBErrorNumber);
            }
        }
        else if (lhsValue is VBNumericTypedValue lhsNumeric)
        {
            var rhsValue = Right.Evaluate(ref scope, rethrow);
            if (rhsValue is VBNumericTypedValue rhsNumeric)
            {
                // both sides are numeric, perform addition
                var resultValue = lhsNumeric.NumericValue + rhsNumeric.NumericValue;

                // return the widest of the two types
                try
                {
                    result = lhsNumeric.Size >= rhsNumeric.Size
                        ? lhsNumeric.WithValue(resultValue)
                        : rhsNumeric.WithValue(resultValue);
                }
                catch (VBRuntimeErrorException vbRuntimeError)
                {
                    if (rethrow)
                    {
                        throw;
                    }
                    // TODO handle adding VBRuntimeErrorException.Overflow to the execution context
                    result = new VBErrorValue().WithValue(vbRuntimeError.VBErrorNumber);
                }
            }
            else
            {
                // RHS is not numeric, VBA would throw a type mismatch at runtime.
                // TODO handle adding VBRuntimeErrorException.TypeMismatch to the execution context
                //scope = scope.WithError(VBRuntimeErrorException.TypeMismatch(this));
                result = new VBErrorValue().WithValue(VBRuntimeErrorException.TypeMismatch(default(Range)!).VBErrorNumber);
            }
        }

        return result;
    }
}

public record class SubtractOpExpression : OperatorExpression
{
    public SubtractOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(Tokens.SubtractionOp, parentUri, [left, right])
    {
        Left = left;
        Right = right;
    }

    public ValuedExpression Left { get; }
    public ValuedExpression Right { get; }

    public override VBTypedValue? Evaluate(ref VBExecutionScope scope, bool rethrow = false)
    {
        VBTypedValue? result = default;

        var lhsValue = Left.Evaluate(ref scope, rethrow);
        if (lhsValue is VBNumericTypedValue lhsNumeric)
        {
            var rhsValue = Right.Evaluate(ref scope, rethrow);
            if (rhsValue is VBNumericTypedValue rhsNumeric)
            {
                // both sides are numeric, perform subtraction
                var resultValue = lhsNumeric.NumericValue - rhsNumeric.NumericValue;

                // return the widest of the two types
                try
                {
                    result = lhsNumeric.Size >= rhsNumeric.Size
                        ? lhsNumeric.WithValue(resultValue)
                        : rhsNumeric.WithValue(resultValue);
                }
                catch (VBRuntimeErrorException vbRuntimeError)
                {
                    if (rethrow)
                    {
                        throw;
                    }
                    // TODO handle adding VBRuntimeErrorException.Overflow to the execution context
                    result = new VBErrorValue().WithValue(vbRuntimeError.VBErrorNumber);
                }
            }
            else
            {
                // RHS is not numeric, VBA would throw a type mismatch at runtime.
                // TODO handle adding VBRuntimeErrorException.TypeMismatch to the execution context
                //scope = scope.WithError(VBRuntimeErrorException.TypeMismatch(this));
                result = new VBErrorValue().WithValue(VBRuntimeErrorException.TypeMismatch(default(Range)!).VBErrorNumber);
            }
        }

        return result;
    }
}

public record class MultiplicationOpExpression : OperatorExpression
{
    public MultiplicationOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(Tokens.SubtractionOp, parentUri, [left, right])
    {
        Left = left;
        Right = right;
    }

    public ValuedExpression Left { get; }
    public ValuedExpression Right { get; }

    public override VBTypedValue? Evaluate(ref VBExecutionScope scope, bool rethrow = false)
    {
        VBTypedValue? result = default;

        var lhsValue = Left.Evaluate(ref scope, rethrow);
        if (lhsValue is VBNumericTypedValue lhsNumeric)
        {
            var rhsValue = Right.Evaluate(ref scope, rethrow);
            if (rhsValue is VBNumericTypedValue rhsNumeric)
            {
                // both sides are numeric, perform multiplication
                var resultValue = lhsNumeric.NumericValue * rhsNumeric.NumericValue;

                // TODO CONFIRM THIS
                // return the widest of the two types
                try
                {
                    result = lhsNumeric.Size >= rhsNumeric.Size
                        ? lhsNumeric.WithValue(resultValue)
                        : rhsNumeric.WithValue(resultValue);
                }
                catch (VBRuntimeErrorException vbRuntimeError)
                {
                    if (rethrow)
                    {
                        throw;
                    }
                    // TODO handle adding VBRuntimeErrorException.Overflow to the execution context
                    result = new VBErrorValue().WithValue(vbRuntimeError.VBErrorNumber);
                }
            }
            else
            {
                // RHS is not numeric, VBA would throw a type mismatch at runtime.
                // TODO handle adding VBRuntimeErrorException.TypeMismatch to the execution context
                //scope = scope.WithError(VBRuntimeErrorException.TypeMismatch(this));
                result = new VBErrorValue().WithValue(VBRuntimeErrorException.TypeMismatch(default(Range)!).VBErrorNumber);
            }
        }

        return result;
    }
}

public record class DivisionOpExpression : OperatorExpression
{
    public DivisionOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(Tokens.DivisionOp, parentUri, [left, right])
    {
        Left = left;
        Right = right;
    }

    public ValuedExpression Left { get; }
    public ValuedExpression Right { get; }

    public override VBTypedValue? Evaluate(ref VBExecutionScope scope, bool rethrow = false)
    {
        VBTypedValue? result = default;

        var lhsValue = Left.Evaluate(ref scope, rethrow);
        if (lhsValue is VBNumericTypedValue lhsNumeric)
        {
            var rhsValue = Right.Evaluate(ref scope, rethrow);
            if (rhsValue is VBNumericTypedValue rhsNumeric)
            {
                // both sides are numeric, perform division
                var resultValue = lhsNumeric.NumericValue / rhsNumeric.NumericValue;

                // TODO CONFIRM THIS
                // return the widest of the two types
                try
                {
                    result = lhsNumeric.Size >= rhsNumeric.Size
                        ? lhsNumeric.WithValue(resultValue)
                        : rhsNumeric.WithValue(resultValue);
                }
                catch (VBRuntimeErrorException vbRuntimeError)
                {
                    if (rethrow)
                    {
                        throw;
                    }
                    // TODO handle adding VBRuntimeErrorException.Overflow to the execution context
                    result = new VBErrorValue().WithValue(vbRuntimeError.VBErrorNumber);
                }
            }
            else
            {
                // RHS is not numeric, VBA would throw a type mismatch at runtime.
                // TODO handle adding VBRuntimeErrorException.TypeMismatch to the execution context
                //scope = scope.WithError(VBRuntimeErrorException.TypeMismatch(this));
                result = new VBErrorValue().WithValue(VBRuntimeErrorException.TypeMismatch(default(Range)!).VBErrorNumber);
            }
        }

        return result;
    }
}
public record class IntegerDivisionOpExpression : OperatorExpression
{
    public IntegerDivisionOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(Tokens.IntegerDivisionOp, parentUri, [left, right])
    {
        Left = left;
        Right = right;
    }

    public ValuedExpression Left { get; }
    public ValuedExpression Right { get; }

    public override VBTypedValue? Evaluate(ref VBExecutionScope scope, bool rethrow = false)
    {
        VBTypedValue? result = default;

        var lhsValue = Left.Evaluate(ref scope, rethrow);
        if (lhsValue is VBNumericTypedValue lhsNumeric)
        {
            var rhsValue = Right.Evaluate(ref scope, rethrow);
            if (rhsValue is VBNumericTypedValue rhsNumeric)
            {
                // both sides are numeric, perform division
                var resultValue = (int)(lhsNumeric.NumericValue / rhsNumeric.NumericValue);

                // TODO CONFIRM THIS
                // return the widest of the two types
                try
                {
                    result = lhsNumeric.Size >= rhsNumeric.Size
                        ? lhsNumeric.WithValue(resultValue)
                        : rhsNumeric.WithValue(resultValue);
                }
                catch (VBRuntimeErrorException vbRuntimeError)
                {
                    if (rethrow)
                    {
                        throw;
                    }
                    // TODO handle adding VBRuntimeErrorException.Overflow to the execution context
                    result = new VBErrorValue().WithValue(vbRuntimeError.VBErrorNumber);
                }
            }
            else
            {
                // RHS is not numeric, VBA would throw a type mismatch at runtime.
                // TODO handle adding VBRuntimeErrorException.TypeMismatch to the execution context
                //scope = scope.WithError(VBRuntimeErrorException.TypeMismatch(this));
                result = new VBErrorValue().WithValue(VBRuntimeErrorException.TypeMismatch(default(Range)!).VBErrorNumber);
            }
        }

        return result;
    }
}
public record class ModulusOpExpression : OperatorExpression
{
    public ModulusOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(Tokens.Mod, parentUri, [left, right])
    {
        Left = left;
        Right = right;
    }

    public ValuedExpression Left { get; }
    public ValuedExpression Right { get; }

    public override VBTypedValue? Evaluate(ref VBExecutionScope scope, bool rethrow = false)
    {
        VBTypedValue? result = default;

        var lhsValue = Left.Evaluate(ref scope, rethrow);
        if (lhsValue is VBNumericTypedValue lhsNumeric)
        {
            var rhsValue = Right.Evaluate(ref scope, rethrow);
            if (rhsValue is VBNumericTypedValue rhsNumeric)
            {
                // both sides are numeric, perform operation
                var resultValue = lhsNumeric.NumericValue % rhsNumeric.NumericValue;

                // TODO CONFIRM THIS
                // return the widest of the two types
                try
                {
                    result = lhsNumeric.Size >= rhsNumeric.Size
                        ? lhsNumeric.WithValue(resultValue)
                        : rhsNumeric.WithValue(resultValue);
                }
                catch (VBRuntimeErrorException vbRuntimeError)
                {
                    if (rethrow)
                    {
                        throw;
                    }
                    // TODO handle adding VBRuntimeErrorException.Overflow to the execution context
                    result = new VBErrorValue().WithValue(vbRuntimeError.VBErrorNumber);
                }
            }
            else
            {
                // RHS is not numeric, VBA would throw a type mismatch at runtime.
                // TODO handle adding VBRuntimeErrorException.TypeMismatch to the execution context
                //scope = scope.WithError(VBRuntimeErrorException.TypeMismatch(this));
                result = new VBErrorValue().WithValue(VBRuntimeErrorException.TypeMismatch(default(Range)!).VBErrorNumber);
            }
        }

        return result;
    }
}
public record class UnaryMinusOpExpression : OperatorExpression
{
    public UnaryMinusOpExpression(WorkspaceUri parentUri, ValuedExpression expression)
        : base(Tokens.SubtractionOp, parentUri, [expression])
    {
        Expression = expression;
    }

    public ValuedExpression Expression { get; }

    public override VBTypedValue? Evaluate(ref VBExecutionScope scope, bool rethrow = false)
    {
        VBTypedValue? result = default;

        if (Expression is NumericValuedExpression numericExpression)
        {
            var value = numericExpression.Evaluate(ref scope, rethrow);
            if (value is VBNumericTypedValue numericValue)
            {
                result = numericValue.WithValue(-1 * value.NumericValue);
            }
            else
            {
                result = new VBErrorValue().WithValue(VBRuntimeErrorException.TypeMismatch(default(Range)!).VBErrorNumber);
            }
        }
        else
        {
            result = new VBErrorValue().WithValue(VBRuntimeErrorException.TypeMismatch(default(Range)!).VBErrorNumber);
        }

        return result;
    }
}



public record class UnaryNotOpExpression : OperatorExpression
{
    public UnaryNotOpExpression(WorkspaceUri parentUri, ValuedExpression expression)
        : base(Tokens.LogicalNotOp, parentUri, [expression])
    {
        Expression = expression;
    }

    public ValuedExpression Expression { get; }

    public override VBTypedValue? Evaluate(ref VBExecutionScope scope, bool rethrow = false)
    {
        VBTypedValue? result = default;

        if (Expression is BooleanValuedExpression boolExpression)
        {
            var value = boolExpression.Evaluate(ref scope, rethrow)!;
            result = new VBBooleanValue().WithValue(!value.Value);
        }
        else if (Expression is NumericValuedExpression numericExpression)
        {
            var value = numericExpression.Evaluate(ref scope, rethrow);
            if (value is VBNumericTypedValue numericValue)
            {
                result = numericValue.WithValue(-1 * value.NumericValue);
            }
            else
            {
                result = new VBErrorValue().WithValue(VBRuntimeErrorException.TypeMismatch(default(Range)!).VBErrorNumber);
            }
        }
        else
        {
            result = new VBErrorValue().WithValue(VBRuntimeErrorException.TypeMismatch(default(Range)!).VBErrorNumber);
        }

        return result;
    }
}


public record class PowOpExpression : OperatorExpression
{
    public PowOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(Tokens.PowerOp, parentUri, [left, right])
    {
        Left = left;
        Right = right;
    }

    public ValuedExpression Left { get; }
    public ValuedExpression Right { get; }

    public override VBTypedValue? Evaluate(ref VBExecutionScope scope, bool rethrow = false)
    {
        VBTypedValue? result = default;

        var lhsValue = Left.Evaluate(ref scope, rethrow);
        if (lhsValue is VBNumericTypedValue lhsNumeric)
        {
            var rhsValue = Right.Evaluate(ref scope, rethrow);
            if (rhsValue is VBNumericTypedValue rhsNumeric)
            {
                // both sides are numeric, perform subtraction
                var resultValue = Math.Pow(lhsNumeric.NumericValue, rhsNumeric.NumericValue);

                // return the widest of the two types
                try
                {
                    result = lhsNumeric.Size >= rhsNumeric.Size
                        ? lhsNumeric.WithValue(resultValue)
                        : rhsNumeric.WithValue(resultValue);
                }
                catch (VBRuntimeErrorException vbRuntimeError)
                {
                    if (rethrow)
                    {
                        throw;
                    }
                    // TODO handle adding VBRuntimeErrorException.Overflow to the execution context
                    result = new VBErrorValue().WithValue(vbRuntimeError.VBErrorNumber);
                }
            }
            else
            {
                // RHS is not numeric, VBA would throw a type mismatch at runtime.
                // TODO handle adding VBRuntimeErrorException.TypeMismatch to the execution context
                //scope = scope.WithError(VBRuntimeErrorException.TypeMismatch(this));
                result = new VBErrorValue().WithValue(VBRuntimeErrorException.TypeMismatch(default(Range)!).VBErrorNumber);
            }
        }

        return result;
    }
}

public record class TypeOfExpression : ValuedExpression
{
    public TypeOfExpression(WorkspaceUri parentUri, ValuedExpression expression)
        : base(RubberduckSymbolKind.Operator, Tokens.TypeOf, parentUri, [expression])
    {
        Expression = expression;
    }
    public ValuedExpression Expression { get; }
    public override VBTypedValue? Evaluate(ref VBExecutionScope scope, bool rethrow = false)
    {
        VBTypedValue? result = default;
        var exprValue = Expression.Evaluate(ref scope, rethrow);
        if (exprValue is VBObjectValue objValue)
        {
            return new VBTypeDescValue(objValue.TypeInfo);
        }
        return result;
    }
}

/// <summary>
/// Represents a literal expression.
/// </summary>
public record class LiteralExpression : ValuedExpression
{
    public LiteralExpression(RubberduckSymbolKind kind, WorkspaceUri parentUri, VBTypedValue value)
        : base(kind, value.ToString(), parentUri)
    {
        Value = value;
    }

    public VBTypedValue Value { get; }

    public override VBTypedValue? Evaluate(ref VBExecutionScope scope, bool rethrow = false) => Value;
}

public record class StringLiteralExpression : LiteralExpression
{
    public StringLiteralExpression(WorkspaceUri parentUri, VBStringValue value)
        : base(RubberduckSymbolKind.StringLiteral, parentUri, value)
    {
    }
}

/// <summary>
/// Represents an explicitly-typed <c>String</c> (<c>$</c>) literal value.
/// </summary>
public record class HintedStringLiteralExpression : StringLiteralExpression
{
    public HintedStringLiteralExpression(WorkspaceUri parentUri, VBStringValue value)
        : base(parentUri, value)
    {
    }
}

/// <summary>
/// Represents the <c>Nothing</c> literal value.
/// </summary>
public record class ObjectLiteralExpression : LiteralExpression
{
    public ObjectLiteralExpression(WorkspaceUri parentUri)
        : base(RubberduckSymbolKind.Nothing, parentUri, VBObjectValue.Nothing)
    {
    }
}

public record class DateLiteralExpression : LiteralExpression
{
    public DateLiteralExpression(WorkspaceUri parentUri, VBDateValue value)
        : base(RubberduckSymbolKind.DateLiteral, parentUri, value)
    {
    }
}

public record class BooleanLiteralExpression : LiteralExpression
{
    public BooleanLiteralExpression(WorkspaceUri parentUri, VBBooleanValue value)
        : base(RubberduckSymbolKind.BooleanLiteral, parentUri, value)
    {
    }
}

public record class HexLiteralExpression : NumericLiteralExpression
{
    public HexLiteralExpression(WorkspaceUri parentUri, VBNumericTypedValue value)
        : base(parentUri, value)
    {
    }
}

public record class OctalLiteralExpression : NumericLiteralExpression
{
    public OctalLiteralExpression(WorkspaceUri parentUri, VBNumericTypedValue value)
        : base(parentUri, value)
    {
    }
}

public abstract record class VariantLiteralExpression : LiteralExpression
{
    protected VariantLiteralExpression(WorkspaceUri parentUri, VBTypedValue value)
        : base(RubberduckSymbolKind.VariantLiteral, parentUri, value)
    {
    }
}

public record class EmptyLiteralExpression : VariantLiteralExpression
{
    public EmptyLiteralExpression(WorkspaceUri parentUri)
        : base(parentUri, VBEmptyValue.Empty)
    {
    }
}

public record class NullLiteralExpression : VariantLiteralExpression
{
    public NullLiteralExpression(WorkspaceUri parentUri)
        : base(parentUri, VBNullValue.Null)
    {
    }
}

/// <summary>
/// Represents an implicitly-typed numeric literal value.
/// </summary>
public record class NumericLiteralExpression : LiteralExpression
{
    public NumericLiteralExpression(WorkspaceUri parentUri, VBNumericTypedValue value)
        : base(RubberduckSymbolKind.NumberLiteral, parentUri, value)
    {
    }
}

/// <summary>
/// Represents an explicitly-typed <c>Integer</c> (<c>%</c>) literal value.
/// </summary>
public record class IntegerLiteralExpression : NumericLiteralExpression
{
    public IntegerLiteralExpression(WorkspaceUri parentUri, VBIntegerValue value)
        : base(parentUri, value)
    {
    }
}

/// <summary>
/// Represents an explicitly-typed <c>LongLong</c> (<c>^</c>) literal value.
/// </summary>
public record class LongLongLiteralExpression : NumericLiteralExpression
{
    public LongLongLiteralExpression(WorkspaceUri parentUri, VBLongLongValue value)
        : base(parentUri, value)
    {
    }
}

/// <summary>
/// Represents an explicitly-typed <c>Long</c> (<c>&</c>) literal value.
/// </summary>
public record class LongLiteralExpression : NumericLiteralExpression
{
    public LongLiteralExpression(WorkspaceUri parentUri, VBLongValue value)
        : base(parentUri, value)
    {
    }
}

/// <summary>
/// Represents an explicitly-typed <c>Currency</c> (<c>@</c>) literal value.
/// </summary>
public record class DecimalLiteralExpression : NumericLiteralExpression
{
    public DecimalLiteralExpression(WorkspaceUri parentUri, VBDecimalValue value)
        : base(parentUri, value)
    {
    }
}

/// <summary>
/// Represents an explicitly-typed <c>Single</c> (<c>!</c>) literal value.
/// </summary>
public record class SingleLiteralExpression : NumericLiteralExpression
{
    public SingleLiteralExpression(WorkspaceUri parentUri, VBSingleValue value)
        : base(parentUri, value)
    {
    }
}

/// <summary>
/// Represents an explicitly-typed <c>Double</c> (<c>#</c>) literal value.
/// </summary>
public record class DoubleLiteralExpression : NumericLiteralExpression
{
    public DoubleLiteralExpression(WorkspaceUri parentUri, VBDoubleValue value)
        : base(parentUri, value)
    {
    }
}


/// <summary>
/// Represents an expression that evaluates to an object reference.
/// </summary>
public abstract record class ObjectValuedExpression : ValuedExpression<VBObjectValue>
{
    protected ObjectValuedExpression(WorkspaceUri parentUri, IEnumerable<ValuedExpression>? children = null)
        : base(RubberduckSymbolKind.Expression, VBObjectType.TypeInfo, Tokens.Object, parentUri, children)
    {
    }
}

/// <summary>
/// Represents a <c>New</c> expression, that creates a new instance of a class.
/// </summary>
public record class NewInstExpression : OperatorExpression<VBObjectValue>
{
    public NewInstExpression(WorkspaceUri parentUri, ClassModuleSymbol classType)
        : base(Tokens.New, classType.ResolvedType!, parentUri, [])
    {
        ClassType = classType;
    }

    public ClassModuleSymbol ClassType { get; }

    public override VBObjectValue? Evaluate(ref VBExecutionScope context, bool rethrow = false)
    {
        return new VBObjectValue(ClassType) { Value = Guid.NewGuid() };
    }
}

/// <summary>
/// Represents an expression that evaluates to a Boolean value.
/// </summary>
public record class BooleanValuedExpression : ValuedExpression<VBBooleanValue>
{
    public BooleanValuedExpression(WorkspaceUri parentUri, IEnumerable<ValuedExpression>? children = null)
        : base(RubberduckSymbolKind.Expression, VBBooleanType.TypeInfo, string.Empty, parentUri, children)
    {
    }
}

/// <summary>
/// Represents an expression that evaluates to a string value.
/// </summary>
public record class StringValuedExpression : ValuedExpression<VBStringValue>
{
    public StringValuedExpression(WorkspaceUri parentUri, IEnumerable<ValuedExpression>? children = null)
        : base(RubberduckSymbolKind.Expression, VBStringType.TypeInfo, string.Empty, parentUri, children)
    {
    }
}

public record class NumericValuedExpression : ValuedExpression<VBNumericTypedValue>
{
    public NumericValuedExpression(WorkspaceUri parentUri, IEnumerable<ValuedExpression>? children = null)
        : base(RubberduckSymbolKind.Expression, VBDoubleType.TypeInfo, string.Empty, parentUri, children)
    {
    }
}

public record class InvalidExpression : ValuedExpression
{
    public InvalidExpression(string name, WorkspaceUri parentUri, IEnumerable<ValuedExpression>? children = null)
        : base(RubberduckSymbolKind.UnknownSymbol, name, parentUri, children)
    {
    }
    public override VBTypedValue? Evaluate(ref VBExecutionScope scope, bool rethrow = false) => null;
}

public record class InvalidSymbol : Symbol
{
    public InvalidSymbol(string name, WorkspaceUri? parentUri = null, IEnumerable<Symbol>? children = null)
        : base(RubberduckSymbolKind.UnknownSymbol, name, parentUri, children: children)
    {
    }
}

/// <summary>
/// Represents a document symbol, as defined by LSP.
/// </summary>
public abstract record class Symbol : DocumentSymbol
{
    protected Symbol(RubberduckSymbolKind kind, string name, WorkspaceUri? parentUri = null, Accessibility accessibility = Accessibility.Undefined, IEnumerable<Symbol>? children = default)
    {
        Kind = (SymbolKind)kind;
        Name = name;
        ParentUri = parentUri ?? new WorkspaceFileUri($"{kind}", new Uri("vb://symbols"));
        Uri = ParentUri.GetChildSymbolUri(name);
        Children = new(children?.Select(e => e.WithParentUri(Uri)) ?? []);
    }

    /// <summary>
    /// <c>true</c> if the symbol is user-defined, <c>false</c> if the symbol is defined in a referenced library.
    /// </summary>
    public bool IsUserDefined { get; init; }
    /// <summary>
    /// The URI of the parent symbol.
    /// </summary>
    /// <remarks>
    /// This information should not be used to construct a symbol hierarchy.
    /// </remarks>
    public WorkspaceUri ParentUri { get; init; }
    /// <summary>
    /// The URI of the symbol as a fragment of the parent URI.
    /// </summary>
    /// <remarks>
    /// Formed with the original string of the parent URI concatenated with a '#' followed by the <c>Name</c> of the symbol.
    /// </remarks>
    public WorkspaceUri Uri { get; init; }

    public Symbol WithName(string name) => this with { Name = name, Uri = ParentUri.GetChildSymbolUri(name) };
    public Symbol WithParentUri(WorkspaceUri parentUri) => this with { Uri = parentUri.GetChildSymbolUri(Name), ParentUri = parentUri };
    public Symbol WithChildren(IEnumerable<Symbol> children) => this with { Children = new(children ?? []) };
}

/// <summary>
/// Represents a symbol that can be resolved to a <c>VBType</c>.
/// </summary>
public abstract record class TypedSymbol : Symbol, ITypedSymbol
{
    public TypedSymbol(RubberduckSymbolKind kind, Accessibility accessibility, string name, WorkspaceUri? parentUri = null, IEnumerable<Symbol>? children = null, VBType? type = null, string? asTypeName = null)
        : base(kind, name, parentUri, accessibility, children)
    {
        Accessibility = accessibility;
        ResolvedType = type ?? ResolveIntrinsicType(asTypeName);
        TypeName = type?.Name ?? asTypeName;
    }

    private VBType? ResolveIntrinsicType(string? name) => VBIntrinsicType.IntrinsicTypes.SingleOrDefault(e => e.Name == name);

    /// <summary>
    /// Determines whether a symbol is accessible beyond its enclosing scope.
    /// </summary>
    /// <remarks>
    /// Usually driven by an access modifier token, but not necessarily.
    /// </remarks>
    public Accessibility Accessibility { get; }

    public string? TypeName { get; init; }

    public VBType? ResolvedType { get; init; }
    public TypedSymbol WithResolvedType(VBType? resolvedType) => this with { ResolvedType = resolvedType };
}

public record class ClassTypeInstanceSymbol : TypedSymbol
{
    public ClassTypeInstanceSymbol(Accessibility accessibility, string name, WorkspaceUri parentUri)
        : base(RubberduckSymbolKind.Class, accessibility, name, parentUri, [], null)
    {
    }
}

public abstract record class DeclarationExpressionSymbol : TypedSymbol, IDeclaredTypeSymbol
{
    protected DeclarationExpressionSymbol(RubberduckSymbolKind kind, string name, WorkspaceUri parentUri, Accessibility accessibility, IEnumerable<Symbol>? children = null, IEnumerable<IParseTreeAnnotation>? annotations = null, string? asTypeExpression = null, VBType? type = null)
        : base(kind, accessibility, name, parentUri, children, ResolveVariantIfUnspecified(type, asTypeExpression), asTypeExpression)
    {
    }

    private static VBType? ResolveVariantIfUnspecified(VBType? type, string? asTypeExpression)
    {
        if (type is null)
        {
            return string.IsNullOrWhiteSpace(asTypeExpression)
                ? VBVariantType.TypeInfo // type not specified is an implicit variant
                : null; // type must be resolved later
        }

        return type;
    }

    public string? AsTypeExpression { get; init; }
}

/// <summary>
/// Represents a symbol that declares a value, e.g. a <c>Const</c>, or an <c>Enum</c> member.
/// </summary>
public abstract record class ValuedTypedSymbol : TypedSymbol, IValuedSymbol
{
    public ValuedTypedSymbol(RubberduckSymbolKind kind, Accessibility accessibility, string name, WorkspaceUri parentUri, string? asTypeExpression, string? valueExpression)
        : base(kind, accessibility, name, parentUri, [])
    {
        ValueExpression = valueExpression;
    }

    /// <summary>
    /// The symbol's declared value expression.
    /// </summary>
    public string? ValueExpression { get; init; }
    /// <summary>
    /// The resolved type of the symbol's value expression.
    /// </summary>
    /// <remarks>
    /// May or may not match the symbol's declared type, or safely convert to it.
    /// </remarks>
    public VBType? ResolvedValueExpressionType { get; init; }

    public abstract VBTypedValue Evaluate(ref VBExecutionScope context, bool rethrow = false);

    public ITypedSymbol WithResolvedValueExpressionType(VBType? type) => this with { ResolvedValueExpressionType = type };
}

public record class ParameterSymbol : DeclarationExpressionSymbol
{
    public ParameterSymbol(string name, WorkspaceUri parentUri, ParameterModifier modifier, string? asTypeExpression)
        : base(RubberduckSymbolKind.Variable, name, parentUri, Accessibility.Private, asTypeExpression: asTypeExpression)
    {
        Modifier = new(modifier);
    }

    /// <summary>
    /// The zero-based position of this parameter in the parameter list.
    /// </summary>
    public int Position { get; init; }
    /// <summary>
    /// Whether this parameter is the last in the parameter list.
    /// </summary>
    public bool IsLast { get; init; }

    /// <summary>
    /// Gets information about this parameter's modifier expression.
    /// </summary>
    public ParameterSymbolModifier Modifier { get; }

    public ParameterSymbol WithPosition(int position) => this with { Position = position };
}

public record class ParamArrayParameterSymbol : ParameterSymbol
{
    public ParamArrayParameterSymbol(string name, WorkspaceUri parentUri, string? asTypeExpression)
        : base(name, parentUri, ParameterModifier.ExplicitByRef, asTypeExpression)
    {
    }
}

public record class OptionalParameterSymbol : ParameterSymbol, IValuedSymbol
{
    public OptionalParameterSymbol(string name, WorkspaceUri parentUri, ParameterModifier modifier, string? asTypeExpression, string? valueExpression)
        : base(name, parentUri, modifier, asTypeExpression)
    {
        ValueExpression = valueExpression;
    }

    public string? ValueExpression { get; init; }
    public VBType? ResolvedValueExpressionType { get; init; }

    public VBTypedValue? Evaluate(ref VBExecutionScope context, bool rethrow = false) => context.GetTypedValue(this);

    public ITypedSymbol WithResolvedValueExpressionType(VBType? resolvedValueExpressionType) => this with { ResolvedValueExpressionType = resolvedValueExpressionType };
}

public record class UserDefinedTypeSymbol : TypedSymbol
{
    public UserDefinedTypeSymbol(string name, WorkspaceUri parentUri, Accessibility accessibility, IEnumerable<UserDefinedTypeMemberSymbol> children)
        : base(RubberduckSymbolKind.UserDefinedType, accessibility, name, parentUri, children.Cast<Symbol>())
    {
        ResolvedType = new VBUserDefinedType(name, parentUri, this);
    }
}

public record class UserDefinedTypeMemberSymbol : DeclarationExpressionSymbol
{
    public UserDefinedTypeMemberSymbol(string name, WorkspaceUri parentUri, string? asTypeExpression)
        : base(RubberduckSymbolKind.Field, name, parentUri, Accessibility.Public, asTypeExpression: asTypeExpression)
    {
    }
}

public record class LibraryFunctionImportSymbol : FunctionSymbol
{
    public LibraryFunctionImportSymbol(string library, string name, string? alias, bool isPtrSafe, WorkspaceUri parentUri, Accessibility accessibility, IEnumerable<ParameterSymbol>? parameters, string? typeName)
        : base(name, parentUri, accessibility, parameters, typeName)
    {
        Library = library;
        OriginalName = name;
        Alias = alias;
        IsPtrSafe = isPtrSafe;
    }

    public bool IsPtrSafe { get; init; }
    public string Library { get; init; }
    public string? OriginalName { get; init; }
    public string? Alias { get; init; }
}

public record class LibraryProcedureImportSymbol : ProcedureSymbol
{
    public LibraryProcedureImportSymbol(string library, string name, string? alias, bool isPtrSafe, WorkspaceUri parentUri, Accessibility accessibility, IEnumerable<ParameterSymbol>? parameters)
        : base(name, parentUri, accessibility, parameters)
    {
        Library = library;
        OriginalName = name;
        Alias = alias;
        IsPtrSafe = isPtrSafe;
    }

    public bool IsPtrSafe { get; init; }
    public string Library { get; init; }
    public string? OriginalName { get; init; }
    public string? Alias { get; init; }
}

public record class FunctionSymbol : TypedSymbol, IExecutable
{
    public FunctionSymbol(string name, WorkspaceUri parentUri, Accessibility accessibility, IEnumerable<Symbol>? children = null, string? typeName = null, RubberduckSymbolKind kind = RubberduckSymbolKind.Function)
        : base(kind, accessibility, name, parentUri, (children ?? []).ToArray(), asTypeName: typeName)
    {
    }

    public bool? IsReachable { get; init; }

    public VBTypedValue? Evaluate(ref VBExecutionScope context, bool rethrow = false)
    {
        // TODO walk the executable symbol tree to track the assigned value
        // for now we're happy just getting a resolved type back
        return context.GetTypedValue(this);
    }

    public VBTypedValue? Execute(ref VBExecutionContext context, bool rethrow = false)
    {
        var member = context.GetModuleMember(this);
        if (member != null)
        {
            var scope = context.EnterScope(member);
            return Evaluate(ref scope);
        }
        throw VBRuntimeErrorException.PropertyOrMethodNotFound(this); // fitting, but is it really a VB-trappable error? or it's a .net-side bug?
    }
}

public record class ProcedureSymbol : TypedSymbol, IExecutable
{
    public ProcedureSymbol(string name, WorkspaceUri parentUri, Accessibility accessibility, IEnumerable<Symbol>? children = null, RubberduckSymbolKind kind = RubberduckSymbolKind.Procedure)
        : base(kind, accessibility, name, parentUri, (children ?? []).ToArray(), VBLongPtrType.TypeInfo) { }

    public VBTypedValue? Evaluate(ref VBExecutionScope context, bool rethrow = false) =>
        context.GetTypedValue(this) as VBLongPtrValue; // symbol table contains a VBLongPtrValue for procedures that can be used with the AddressOf operator.

    public VBTypedValue? Execute(ref VBExecutionContext context, bool rethrow = false)
    {
        var member = context.GetModuleMember(this);
        if (member != null)
        {
            try
            {
                var scope = context.EnterScope(member);
                scope.Execute(ref context);
            }
            catch (VBCompileErrorException vbCompileError)
            {
                context.Diagnostics.Add(vbCompileError.Diagnostic);
                context.End();
            }
            catch (VBRuntimeErrorException vbRuntimeError)
            {
                // unhandled runtime error
                context.Diagnostics.Add(vbRuntimeError.Diagnostic);
            }
            return null;
        }
        throw VBRuntimeErrorException.PropertyOrMethodNotFound(this); // fitting, but is it really a VB-trappable error? or it's a .net-side bug?
    }
}

public record class PropertyGetSymbol : FunctionSymbol
{
    public PropertyGetSymbol(string name, WorkspaceUri parentUri, Accessibility accessibility, IEnumerable<Symbol>? children = null, string? asTypeNameExpression = null)
        : base(name, parentUri, accessibility, (children ?? []).ToArray(), asTypeNameExpression, RubberduckSymbolKind.Property) { }
}

public record class PropertyLetSymbol : ProcedureSymbol
{
    public PropertyLetSymbol(string name, WorkspaceUri parentUri, Accessibility accessibility, IEnumerable<Symbol>? children = null)
        : base(name, parentUri, accessibility, (children ?? []).ToArray(), RubberduckSymbolKind.Property) { }
}

public record class PropertySetSymbol : ProcedureSymbol
{
    public PropertySetSymbol(string name, WorkspaceUri parentUri, Accessibility accessibility, IEnumerable<Symbol>? children = null)
        : base(name, parentUri, accessibility, (children ?? []).ToArray(), RubberduckSymbolKind.Property) { }
}

public record class EnumSymbol : TypedSymbol
{
    public EnumSymbol(string name, WorkspaceUri parentUri, Accessibility accessibility, IEnumerable<EnumMemberSymbol>? children = null, bool isUserDefined = false)
        : base(RubberduckSymbolKind.Enum, accessibility, name, parentUri, children)
    {
        ResolvedType = new VBEnumType(name, parentUri, this, isUserDefined: isUserDefined);
    }
}

public record class EnumMemberSymbol : ValuedTypedSymbol
{
    public EnumMemberSymbol(string name, WorkspaceUri parentUri, string? value)
        : base(RubberduckSymbolKind.EnumMember, Accessibility.Public, name, parentUri, null, value)
    {
        ResolvedType = VBLongType.TypeInfo;
    }

    public override VBTypedValue Evaluate(ref VBExecutionScope context, bool rethrow = false) => context.GetTypedValue(this);
}

public record class EventSymbol : ProcedureSymbol
{
    public EventSymbol(string name, WorkspaceUri parentUri, Accessibility accessibility, IEnumerable<ParameterSymbol>? parameters = default)
        : base(name, parentUri, accessibility, parameters, RubberduckSymbolKind.Event) { }
}

public record class ConstantDeclarationSymbol : ValuedTypedSymbol
{
    public ConstantDeclarationSymbol(string name, WorkspaceUri parentUri, Accessibility accessibility, string? asTypeNameExpression, string? valueExpression)
        : base(RubberduckSymbolKind.Constant, accessibility, name, parentUri, asTypeNameExpression, valueExpression) { }

    public override VBTypedValue Evaluate(ref VBExecutionScope context, bool rethrow = false) => context.GetTypedValue(this);
}

public record class VariableDeclarationSymbol : DeclarationExpressionSymbol
{
    public VariableDeclarationSymbol(string name, WorkspaceUri parentUri, Accessibility accessibility, string? asTypeNameExpression)
        : base(RubberduckSymbolKind.Variable, name, parentUri, accessibility, children: [], annotations: [], asTypeNameExpression) { }
}

public record class StringLiteralSymbol : TypedSymbol
{
    public StringLiteralSymbol(string name, WorkspaceUri parentUri)
        : base(RubberduckSymbolKind.StringLiteral, Accessibility.Undefined, name, parentUri, null, VBStringType.TypeInfo)
    {
    }
}

public record class NumberLiteralSymbol : TypedSymbol
{
    public NumberLiteralSymbol(string name, WorkspaceUri parentUri)
        : base(RubberduckSymbolKind.NumberLiteral, Accessibility.Undefined, name, parentUri, null)
    {
    }
}

public abstract record class OperatorSymbol : TypedSymbol, IValuedExpression<VBTypedValue>
{
    public OperatorSymbol(string token, WorkspaceUri parentUri, IEnumerable<Symbol>? children)
        : base(RubberduckSymbolKind.Operator, Accessibility.Undefined, token, parentUri, children, null)
    {
    }

    public VBTypedValue? Evaluate(ref VBExecutionScope context, bool rethrow = false)
    {
        try
        {
            return EvaluateResult(ref context);
        }
        catch (VBCompileErrorException vbCompileError)
        {
            context = context.WithDiagnostics(vbCompileError.Diagnostics);
            if (rethrow)
            {
                throw;
            }
            return null;
        }
        catch (VBRuntimeErrorException vbRuntimeError)
        {
            context = context.WithError(vbRuntimeError);
            if (rethrow)
            {
                throw;
            }
            return null;
        }
    }

    protected abstract VBTypedValue? EvaluateResult(ref VBExecutionScope context);
}

/// <summary>
/// Represents an executable VBA statement.
/// </summary>
public abstract record class ExecutableStatement : IExecutable
{
    protected ExecutableStatement(WorkspaceUri parentUri)
    {
        ParentUri = parentUri;
    }

    /// <summary>
    /// The URI of the parent symbol.
    /// </summary>
    /// <remarks>
    /// This information should not be used to construct a symbol hierarchy.
    /// </remarks>
    public WorkspaceUri ParentUri { get; init; }

    /// <summary>
    /// Evaluates the statement in the given scope.
    /// </summary>
    public abstract VBTypedValue? Evaluate(ref VBExecutionScope scope, bool rethrow = false);

    /// <summary>
    /// Executes the statement in the given context.
    /// </summary>
    public virtual VBTypedValue? Execute(ref VBExecutionContext context, bool rethrow = false)
    {
        var scope = context.CurrentScope;
        return Evaluate(ref scope, rethrow);
    }
}

/// <summary>
/// Represents an executable call statement.
/// </summary>
public record class CallStatement : ExecutableStatement
{
    protected CallStatement(WorkspaceUri parentUri, IExecutable target)
        : base(parentUri)
    {
        Target = target;
    }

    public IExecutable Target { get; }

    public override VBTypedValue? Evaluate(ref VBExecutionScope scope, bool rethrow = false) => Target.Evaluate(ref scope, rethrow);

    public override VBTypedValue? Execute(ref VBExecutionContext context, bool rethrow = false) => Target.Execute(ref context, rethrow);
}

/// <summary>
/// Represents a block statement, that can contain child executable statements.
/// </summary>
public abstract record class BlockStatement : ExecutableStatement
{
    public BlockStatement(WorkspaceUri parentUri, IEnumerable<IExecutable>? children = null)
        : base(parentUri)
    {
        Children = children ?? [];
    }

    public IEnumerable<IExecutable> Children { get; }

    public override VBTypedValue? Execute(ref VBExecutionContext context, bool rethrow = false)
    {
        var success = ExecuteBody(ref context, rethrow);
        return new VBBooleanValue().WithValue(success);
    }

    protected bool ExecuteBody(ref VBExecutionContext context, bool rethrow = false)
    {
        try
        {
            foreach (var child in Children)
            {
                _ = child.Execute(ref context, rethrow);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public record class IfStatement : BlockStatement
{
    public IfStatement(WorkspaceUri parentUri, BooleanValuedExpression condition, IEnumerable<IExecutable> body)
        : base(parentUri, body)
    {
        Condition = condition;
    }

    public BooleanValuedExpression Condition { get; }

    public override VBTypedValue? Evaluate(ref VBExecutionScope scope, bool rethrow = false) => Condition.Evaluate(ref scope, rethrow);
}

public record class ElseIfStatement : BlockStatement
{
    public ElseIfStatement(WorkspaceUri parentUri, BooleanValuedExpression condition, IEnumerable<IExecutable> body)
        : base(parentUri, body)
    {
        Condition = condition;
    }

    public BooleanValuedExpression Condition { get; }

    public override VBTypedValue? Evaluate(ref VBExecutionScope scope, bool rethrow = false) => Condition.Evaluate(ref scope, rethrow);
}

public record class ElseStatement : BlockStatement
{
    public ElseStatement(WorkspaceUri parentUri, IEnumerable<IExecutable> body)
        : base(parentUri, body) { }

    public override VBTypedValue? Evaluate(ref VBExecutionScope scope, bool rethrow = false) => default;
}

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

    public override VBTypedValue? Evaluate(ref VBExecutionScope scope, bool rethrow = false)
    {
        var result = false;

        var stepValue = StepExpression?.Evaluate(ref scope, rethrow) ?? ImplicitStepValue;
        var increment = ((VBNumericTypedValue)stepValue).NumericValue;

        var toValue = ToExpression.Evaluate(ref scope, rethrow);
        if (toValue is VBNumericTypedValue numericToValue)
        {
            result = increment >= 0
                ? ((VBNumericTypedValue)scope.GetTypedValue(ControlVariable)).NumericValue <= numericToValue.NumericValue
                : ((VBNumericTypedValue)scope.GetTypedValue(ControlVariable)).NumericValue >= numericToValue.NumericValue;
        }

        return new VBBooleanValue().WithValue(result);
    }

    public override VBTypedValue? Execute(ref VBExecutionContext context, bool rethrow = false)
    {
        var didEnter = false;

        var scope = context.CurrentScope;
        var initialValue = FromExpression.Evaluate(ref scope, rethrow);
        if (initialValue is VBNumericTypedValue numericInitialValue)
        {
            scope.SetTypedValue(ControlVariable, numericInitialValue);
        }

        var stepValue = StepExpression?.Evaluate(ref scope, rethrow) ?? ImplicitStepValue;
        var increment = ((VBNumericTypedValue)stepValue).NumericValue;

        var toValue = ToExpression.Evaluate(ref scope, rethrow);
        if (toValue is VBNumericTypedValue numericToValue)
        {
            while (Evaluate(ref scope, rethrow) is VBBooleanValue vbBool && vbBool.Value)
            {
                didEnter = true;
                ExecuteBody(ref context, rethrow); // TODO handle Exit For

                //var currentControlValue = (scope.GetTypedValue(ControlVariable.ReferencedSymbol) as VBNumericTypedValue)!;
                //var newControlValue = currentControlValue.WithValue(currentControlValue.NumericValue + increment);

                scope.SetTypedValue(ControlVariable, toValue); // toValue instead of newControlValue to AVOID ACTUALLY LOOPING
                break; // explicit break to make the above intended behavior more obvious
            }
        }

        return new VBBooleanValue().WithValue(didEnter);
    }
}

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

    public override VBTypedValue? Evaluate(ref VBExecutionScope scope, bool rethrow = false)
    {
        var result = false;

        if (InExpression.Evaluate(ref scope, rethrow) is VBObjectValue objValue)
        {
            result = objValue.TypeInfo is VBCollectionType;
        }

        return new VBBooleanValue().WithValue(result);
    }
}

public enum DoLoopType
{
    /// <summary>
    /// An unconditioned Do...Loop block
    /// </summary>
    DoLoop,
    /// <summary>
    /// Preconditional Do While...Loop block
    /// </summary>
    DoWhile,
    /// <summary>
    /// Preconditional Do Until...Loop block
    /// </summary>
    DoUntil,
    /// <summary>
    /// Postconditional Do... Loop While block
    /// </summary>
    LoopWhile,
    /// <summary>
    /// Postconditional Do... Loop Until block
    /// </summary>
    LoopUntil
}

public abstract record class DoLoopBlockStatement : BlockStatement
{
    protected DoLoopBlockStatement(WorkspaceUri parentUri, IEnumerable<IExecutable>? children = null, DoLoopType loopType = DoLoopType.DoLoop, BooleanValuedExpression? condition = null)
        : base(parentUri, children)
    {
        LoopType = loopType;
        if (loopType != DoLoopType.DoLoop)
        {
            Condition = condition;
        }
    }

    public DoLoopType LoopType { get; }
    public BooleanValuedExpression? Condition { get; }

    public override VBTypedValue? Evaluate(ref VBExecutionScope scope, bool rethrow = false)
    {
        return Condition?.Evaluate(ref scope, rethrow) ?? VBBooleanValue.True;
    }

    public override VBTypedValue? Execute(ref VBExecutionContext context, bool rethrow = false)
    {
        var didEnter = false;
        var scope = context.CurrentScope;
        switch (LoopType)
        {
            case DoLoopType.DoLoop:
                didEnter = true;
                ExecuteBody(ref context, rethrow);
                break; // unconditioned loop, execute body once

            case DoLoopType.DoUntil:
            case DoLoopType.DoWhile:
                while (Evaluate(ref scope, rethrow) is VBBooleanValue vbBool && !vbBool.Value)
                {
                    didEnter = true;
                    ExecuteBody(ref context, rethrow);
                    break; // only execute once for now
                }
                break;

            case DoLoopType.LoopUntil:
            case DoLoopType.LoopWhile:
                do
                {
                    didEnter = true;
                    ExecuteBody(ref context, rethrow);
                    break; // only execute once for now
                }
                while (Evaluate(ref scope, rethrow) is VBBooleanValue vbBool && vbBool.Value);
                break;

            default:
                break;
        }

        return new VBBooleanValue().WithValue(didEnter);
    }
}

/// <summary>
/// Represents an unconditioned Do...Loop executable block.
/// </summary>
public sealed record class DoLoopStatement : DoLoopBlockStatement
{
    public DoLoopStatement(WorkspaceUri parentUri, IEnumerable<IExecutable>? children = null)
        : base(parentUri, children, DoLoopType.DoLoop)
    {
    }
}

/// <summary>
/// Represents a preconditional Do... Loop While executable block.
/// </summary>
public sealed record class DoWhileLoopStatement : DoLoopBlockStatement
{
    public DoWhileLoopStatement(WorkspaceUri parentUri, BooleanValuedExpression condition, IEnumerable<IExecutable>? children = null)
        : base(parentUri, children, DoLoopType.DoWhile, condition)
    {
    }
}

/// <summary>
/// Represents a preconditional Do... Loop Until executable block.
/// </summary>
public sealed record class DoUntilLoopStatement : DoLoopBlockStatement
{
    public DoUntilLoopStatement(WorkspaceUri parentUri, BooleanValuedExpression condition, IEnumerable<IExecutable>? children = null)
        : base(parentUri, children, DoLoopType.DoUntil, condition)
    {
    }
}

/// <summary>
/// Represents a postconditional Do... Loop While executable block.
/// </summary>
public sealed record class LoopWhileDoStatement : DoLoopBlockStatement
{
    public LoopWhileDoStatement(WorkspaceUri parentUri, BooleanValuedExpression condition, IEnumerable<IExecutable>? children = null)
        : base(parentUri, children, DoLoopType.LoopWhile, condition)
    {
    }
}

/// <summary>
/// Represents a postconditional Do... Loop Until executable block.
/// </summary>
public sealed record class LoopUntilDoStatement : DoLoopBlockStatement
{
    public LoopUntilDoStatement(WorkspaceUri parentUri, BooleanValuedExpression condition, IEnumerable<IExecutable>? children = null)
        : base(parentUri, children, DoLoopType.LoopUntil, condition)
    {
    }
}

public record class WhileWendStatement : BlockStatement
{
    public WhileWendStatement(BooleanValuedExpression condition, WorkspaceUri parentUri, IEnumerable<IExecutable>? children = null)
        : base(parentUri, children)
    {
        Condition = condition;
    }

    public BooleanValuedExpression Condition { get; init; }

    public override VBTypedValue? Evaluate(ref VBExecutionScope scope, bool rethrow = false)
    {
        throw new NotImplementedException();
    }
}

public record class WithStatementSymbol : BlockStatement
{
    public WithStatementSymbol(ObjectValuedExpression targetExpression, WorkspaceUri parentUri, IEnumerable<IExecutable>? children = null)
        : base(parentUri, children)
    {
        TargetExpression = targetExpression;
    }

    public ObjectValuedExpression TargetExpression { get; init; }

    public override VBTypedValue? Evaluate(ref VBExecutionScope scope, bool rethrow = false) => TargetExpression.Evaluate(ref scope, rethrow);
}
