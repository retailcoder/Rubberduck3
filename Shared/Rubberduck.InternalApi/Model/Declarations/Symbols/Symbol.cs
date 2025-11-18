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

public interface IExecutable : IExecutable<VBTypedValue>
{
}

public interface IExecutable<TValue> where TValue : VBTypedValue
{
    /// <summary>
    /// Executes the symbol and its children, in the given context.
    /// </summary>
    /// <returns>
    /// Returns a <c>TValue</c> representing the result of the expression; <c>null</c> if the symbol is a non-returning executable member.
    /// </returns>
    TValue? Execute(VBExecutionContext context, bool rethrow = false);
}

public abstract record class ValuedExpression : TypedSymbol, IExecutable<VBTypedValue>
{
    protected ValuedExpression(RubberduckSymbolKind kind, VBType vbType, string name, WorkspaceUri parentUri, IEnumerable<ValuedExpression>? children = null)
        : base(kind, Accessibility.Undefined, name, parentUri, children, vbType)
    {
    }

    public virtual VBTypedValue? Execute(VBExecutionContext context, bool rethrow = false) => default;
}

public record class TypeDescValuedExpression : ValuedExpression<VBTypeDescValue>
{
    public TypeDescValuedExpression(WorkspaceUri parentUri, VBType type)
        : base(RubberduckSymbolKind.Class, VBTypeDesc.TypeInfo, type.Name, parentUri)
    {
        ReferredType = type;
    }
    public VBType ReferredType { get; }
    public override VBTypeDescValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        return new VBTypeDescValue(ReferredType);
    }
}

public abstract record class ValuedExpression<TValue> : ValuedExpression, IExecutable<TValue>
    where TValue : VBTypedValue
{
    protected ValuedExpression(RubberduckSymbolKind kind, VBType vbType, string name, WorkspaceUri parentUri, IEnumerable<ValuedExpression>? children = null)
        : base(kind, vbType, name, parentUri, children)
    {
        Type = vbType;
    }

    public VBType Type { get; init; }

    public override TValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        return Children?.OfType<ValuedExpression<TValue>>().FirstOrDefault()?.Execute(context, true);
    }
}

public abstract record class OperatorExpression<TValue> : ValuedExpression<TValue>
    where TValue : VBTypedValue
{
    protected OperatorExpression(string name, VBType vbType, WorkspaceUri parentUri, IEnumerable<ValuedExpression> children)
        : base(RubberduckSymbolKind.Operator, vbType, name, parentUri, children)
    {
    }
}

public abstract record class BinaryOperatorExpression<TValue> : OperatorExpression<TValue>
    where TValue : VBTypedValue
{
    protected BinaryOperatorExpression(string name, VBType vbType, WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(name, vbType, parentUri, [left, right])
    {
        Left = left;
        Right = right;
    }

    public ValuedExpression Left { get; }
    public ValuedExpression Right { get; }
}

public abstract record class BooleanOperatorExpression : BinaryOperatorExpression<VBBooleanValue>
{
    protected BooleanOperatorExpression(string name, WorkspaceUri parentUri, ValuedExpression lhs, ValuedExpression rhs)
        : base(name, VBBooleanType.TypeInfo, parentUri, lhs, rhs)
    {
    }
}

public abstract record class BitwiseOpExpression : BinaryOperatorExpression<VBLongValue>
{
    public BitwiseOpExpression(WorkspaceUri parentUri, string op, ValuedExpression left, ValuedExpression right)
        : base(op, VBLongType.TypeInfo, parentUri, left, right)
    {
    }

    protected abstract Func<int, int, int> BitwiseOp { get; }

    public override VBLongValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        var lhs = Left.Execute(context, rethrow);
        var intLhs = 0;
        if (lhs is VBBooleanValue vbBoolL)
        {
            intLhs = vbBoolL.Value ? -1 : 0;
        }
        else if (lhs is VBNumericTypedValue vbNumL)
        {
            intLhs = (int)vbNumL.NumericValue;
        }

        var rhs = Right.Execute(context, rethrow);
        var intRhs = 0;
        if (rhs is VBBooleanValue vbBoolR)
        {
            intRhs = vbBoolR.Value ? -1 : 0;
        }
        else if (rhs is VBNumericTypedValue vbNumR)
        {
            intRhs = (int)vbNumR.NumericValue;
        }

        var result = BitwiseOp(intLhs, intRhs);
        return new VBLongValue().WithValue(result);
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

    protected abstract bool CompareStringOp(string left, string right, StringComparison stringComparison);
    protected abstract bool CompareNumberOp(double left, double right);

    public override VBBooleanValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        VBBooleanValue? result = default;

        var rhsValue = Right.Execute(context, rethrow);
        var lhsValue = Left.Execute(context, rethrow);

        if (lhsValue is VBNumericTypedValue lhsNumeric)
        {
            if (rhsValue is VBNumericTypedValue rhsNumeric)
            {
                result = SymbolOperation.ExecuteCompareOpResult(context, this, lhsNumeric, rhsNumeric, CompareNumberOp);
            }
            else if (rhsValue?.TypeInfo.ConvertsSafelyToType(lhsValue.TypeInfo) ?? false)
            {
                var coercedRhs = rhsValue.AsVariant().AsCoercedNumeric()?.NumericValue;
                result = SymbolOperation.ExecuteCompareOpResult(context, this, lhsNumeric, rhsValue, CompareNumberOp);
            }
            else
            {
                var exception = VBRuntimeErrorException.TypeMismatch(Right);
                context.AddDiagnostics(exception);
                if (rethrow)
                {
                    throw exception;
                }
            }
        }
        else if (lhsValue is VBStringValue lhsString)
        {
            if (rhsValue is VBStringValue rhsString)
            {
                result = SymbolOperation.ExecuteCompareOpResult(context, this, lhsString, rhsString, CompareStringOp);
            }
            else if (rhsValue?.TypeInfo.ConvertsSafelyToType(lhsValue.TypeInfo) ?? false)
            {
                var coercedRhs = rhsValue.AsVariant().AsCoercedString();
                result = SymbolOperation.ExecuteCompareOpResult(context, this, lhsString, rhsValue, CompareStringOp);
            }
        }

        return result;
    }
}

public record class EqCompareOpExpression : CompareOpExpression
{
    public EqCompareOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(parentUri, Tokens.CompareEqualOp, left, right)
    {
    }

    protected override bool CompareNumberOp(double left, double right) => left.Equals(right);

    protected override bool CompareStringOp(string left, string right, StringComparison stringComparison) => left.Equals(right, stringComparison);
}
public record class NeqCompareOpExpression : CompareOpExpression
{
    public NeqCompareOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(parentUri, Tokens.CompareNotEqualOp, left, right)
    {
    }

    protected override bool CompareNumberOp(double left, double right) => !left.Equals(right); // TODO !.VBEquals(right)

    protected override bool CompareStringOp(string left, string right, StringComparison stringComparison) => !left.Equals(right, stringComparison);
}
public record class LtCompareOpExpression : CompareOpExpression
{
    public LtCompareOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(parentUri, Tokens.CompareLessThanOp, left, right)
    {
    }

    protected override bool CompareNumberOp(double left, double right) => Comparer<double>.Default.Compare(left, right) < 0;

    protected override bool CompareStringOp(string left, string right, StringComparison stringComparison) => string.Compare(left, right, stringComparison) < 0;
}
public record class LEqCompareOpExpression : CompareOpExpression
{
    public LEqCompareOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(parentUri, Tokens.CompareLessThanOrEqualOp, left, right)
    {
    }

    protected override bool CompareNumberOp(double left, double right) => Comparer<double>.Default.Compare(left, right) <= 0;

    protected override bool CompareStringOp(string left, string right, StringComparison stringComparison) => string.Compare(left, right, stringComparison) <= 0;
}
public record class GtCompareOpExpression : CompareOpExpression
{
    public GtCompareOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(parentUri, Tokens.CompareGreaterThanOp, left, right)
    {
    }

    protected override bool CompareNumberOp(double left, double right) => Comparer<double>.Default.Compare(left, right) > 0;
    protected override bool CompareStringOp(string left, string right, StringComparison stringComparison) => string.Compare(left, right, stringComparison) > 0;
}
public record class GEqCompareOpExpression : CompareOpExpression
{
    public GEqCompareOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(parentUri, Tokens.CompareGreaterThanOrEqualOp, left, right)
    {
    }

    protected override bool CompareNumberOp(double left, double right) => Comparer<double>.Default.Compare(left, right) >= 0;
    protected override bool CompareStringOp(string left, string right, StringComparison stringComparison) => string.Compare(left, right, stringComparison) >= 0;
}

public record class LikeCompareOpExpression : CompareOpExpression
{
    public LikeCompareOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(parentUri, Tokens.CompareLikeOp, left, right)
    {
    }
    protected override bool CompareNumberOp(double left, double right) => Comparer<double>.Default.Compare(left, right) >= 0;
    protected override bool CompareStringOp(string left, string right, StringComparison stringComparison) => string.Compare(left, right, stringComparison) >= 0;
}

public record class IsCompareOpExpression : CompareOpExpression
{
    public IsCompareOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(parentUri, Tokens.CompareIsOp, left, right)
    {
    }

    // NOTE: there would be a type mismatch on LHS symbol given a VBObjectValue RHS
    // These overrides are obligatory, but will not be invoked since we're overriding Execute

    protected override bool CompareStringOp(string left, string right, StringComparison stringComparison) => throw VBCompileErrorException.TypeMismatch(Right, "An object reference is expected in this context");

    protected override bool CompareNumberOp(double left, double right) => throw VBCompileErrorException.TypeMismatch(Right, "An object reference is expected in this context");

    public override VBBooleanValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        var rhs = Right.Execute(context, rethrow);
        var lhs = Left.Execute(context, rethrow);

        if (rhs?.TypeInfo is VBTypeDesc typeofRhs)
        {
            if (lhs?.TypeInfo is VBTypeDesc typeofLhs)
            {
                var result = typeofLhs.Equals(typeofRhs);
                return new VBBooleanValue(this) { Value = result };
            }
            else
            {
                throw VBCompileErrorException.TypeMismatch(Left);
            }

        }
        else if (Right.Type is VBObjectType)
        {
            throw VBCompileErrorException.TypeMismatch(Right);
        }

        // TODO
        throw new NotImplementedException();

        //return base.Execute(context, rethrow);
    }
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

    public override VBStringValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        var rhs = Right.Execute(context, rethrow) as VBStringValue;
        var lhs = Left.Execute(context, rethrow) as VBStringValue;

        // TODO handle adding VBRuntimeErrorException.TypeMismatch to the execution context

        var result = $"{lhs?.Value}{rhs?.Value}";
        return new VBStringValue().WithValue(result);
    }
}

public record class ParenthesizedExpression : OperatorExpression<VBTypedValue>
{
    public ParenthesizedExpression(WorkspaceUri parentUri, ValuedExpression innerExpression)
        : base("LET_COERCE", innerExpression.Type!, parentUri, [innerExpression])
    {
        InnerExpression = innerExpression;
    }
    public ValuedExpression InnerExpression { get; }
    public override VBTypedValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        var value = InnerExpression.Execute(context, rethrow);
        if (value is VBObjectValue objectValue)
        {
            try
            {
                return objectValue.LetCoerce();
            }
            catch (VBRuntimeErrorException exception)
            {
                context.AddDiagnostics(exception);
                if (rethrow)
                {
                    throw;
                }
            }
        }
        else
        {
            return value;
        }

        return default;
    }
}

public record class AddOpExpression : BinaryOperatorExpression<VBTypedValue>
{
    public AddOpExpression(WorkspaceUri parentUri, StringValuedExpression left, StringValuedExpression right)
        : base(Tokens.AdditionOp, VBStringType.TypeInfo, parentUri, left, right)
    {
    }
    public AddOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(Tokens.AdditionOp, VBDoubleType.TypeInfo, parentUri, left, right)
    {
    }

    public override VBTypedValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        VBTypedValue? result = default;

        var lhsValue = Left.Execute(context, rethrow);
        if (lhsValue is VBStringValue lhsString)
        {
            // if LHS coerces to a string, RHS is coerced to string and concatenated
            var rhsValue = Right.Execute(context, rethrow);
            if (rhsValue?.TypeInfo.ConvertsSafelyToType(VBStringType.TypeInfo) ?? false)
            {
                // TODO safely convert value to string

                if (rhsValue is VBStringValue rhsString)
                {
                    var resultValue = $"{lhsString.Value}{rhsString.Value}";
                    result = new VBStringValue(this) { Value = resultValue };
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
            var rhsValue = Right.Execute(context, rethrow);
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

public record class SubtractOpExpression : BinaryOperatorExpression<VBTypedValue>
{
    public SubtractOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(Tokens.SubtractionOp, VBDoubleType.TypeInfo, parentUri, left, right)
    {
    }

    public override VBTypedValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        VBTypedValue? result = default;

        var lhsValue = Left.Execute(context, rethrow);
        if (lhsValue is VBNumericTypedValue lhsNumeric)
        {
            var rhsValue = Right.Execute(context, rethrow);
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
                    context.AddDiagnostics(vbRuntimeError);
                    if (rethrow)
                    {
                        throw;
                    }
                }
            }
            else
            {
                // RHS is not numeric, VBA would throw a type mismatch at runtime.
                var exception = VBRuntimeErrorException.TypeMismatch(this);
                context.AddDiagnostics(exception);
                if (rethrow)
                {
                    throw exception;
                }
            }
        }

        return result;
    }
}

public record class MultiplicationOpExpression : BinaryOperatorExpression<VBTypedValue>
{
    public MultiplicationOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(Tokens.SubtractionOp, left.Type, parentUri, left, right)
    {
    }

    public override VBTypedValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        VBTypedValue? result = default;

        var lhsValue = Left.Execute(context, rethrow);
        if (lhsValue is VBNumericTypedValue lhsNumeric)
        {
            var rhsValue = Right.Execute(context, rethrow);
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
                    context.AddDiagnostics(vbRuntimeError);
                    if (rethrow)
                    {
                        throw;
                    }
                }
            }
            else
            {
                // RHS is not numeric, VBA would throw a type mismatch at runtime.
                var exception = VBRuntimeErrorException.TypeMismatch(this);
                context.AddDiagnostics(exception);
                if (rethrow)
                {
                    throw exception;
                }
            }
        }

        return result;
    }
}

public record class DivisionOpExpression : BinaryOperatorExpression<VBTypedValue>
{
    public DivisionOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(Tokens.DivisionOp, left.Type, parentUri, left, right)
    {
    }

    public override VBTypedValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        VBTypedValue? result = default;

        var lhsValue = Left.Execute(context, rethrow);
        if (lhsValue is VBNumericTypedValue lhsNumeric)
        {
            var rhsValue = Right.Execute(context, rethrow);
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
                    context.AddDiagnostics(vbRuntimeError);
                    if (rethrow)
                    {
                        throw;
                    }
                }
            }
            else
            {
                // RHS is not numeric, VBA would throw a type mismatch at runtime.
                var exception = VBRuntimeErrorException.TypeMismatch(this);
                context.AddDiagnostics(exception);
                if (rethrow)
                {
                    throw exception;
                }
            }
        }

        return result;
    }
}
public record class IntegerDivisionOpExpression : BinaryOperatorExpression<VBTypedValue>
{
    public IntegerDivisionOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(Tokens.IntegerDivisionOp, left.Type, parentUri, left, right)
    {
    }

    public override VBTypedValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        VBTypedValue? result = default;

        var lhsValue = Left.Execute(context, rethrow);
        if (lhsValue is VBNumericTypedValue lhsNumeric)
        {
            var rhsValue = Right.Execute(context, rethrow);
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
                    context.AddDiagnostics(vbRuntimeError);
                    if (rethrow)
                    {
                        throw;
                    }
                }
            }
            else
            {
                // RHS is not numeric, VBA would throw a type mismatch at runtime.
                var exception = VBRuntimeErrorException.TypeMismatch(this);
                context.AddDiagnostics(exception);
                if (rethrow)
                {
                    throw exception;
                }
            }
        }

        return result;
    }
}
public record class ModulusOpExpression : BinaryOperatorExpression<VBTypedValue>
{
    public ModulusOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(Tokens.Mod, VBDoubleType.TypeInfo, parentUri, left, right)
    {
    }

    public override VBTypedValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        VBTypedValue? result = default;

        var lhsValue = Left.Execute(context, rethrow);
        if (lhsValue is VBNumericTypedValue lhsNumeric)
        {
            var rhsValue = Right.Execute(context, rethrow);
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
                    context.AddDiagnostics(vbRuntimeError);
                    if (rethrow)
                    {
                        throw;
                    }
                }
            }
            else
            {
                // RHS is not numeric, VBA would throw a type mismatch at runtime.
                var exception = VBRuntimeErrorException.TypeMismatch(this);
                context.AddDiagnostics(exception);
                if (rethrow)
                {
                    throw exception;
                }
            }
        }

        return result;
    }
}

public record class UnaryMinusOpExpression : OperatorExpression<VBTypedValue>
{
    public UnaryMinusOpExpression(WorkspaceUri parentUri, ValuedExpression expression)
        : base(Tokens.SubtractionOp, VBDoubleType.TypeInfo, parentUri, [expression])
    {
        Expression = expression;
    }

    public ValuedExpression Expression { get; }

    public override VBTypedValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        var expression = Expression.Execute(context, rethrow);
        if (expression is VBNumericTypedValue value)
        {
            var newValue = -value.NumericValue;
            return new VBDoubleValue(this) { NumericValue = newValue };
        }

        var exception = VBRuntimeErrorException.TypeMismatch(this);
        context.AddDiagnostics(exception);
        if (rethrow)
        {
            throw exception;
        }

        return default;
    }
}

public record class UnaryNotOpExpression : OperatorExpression<VBTypedValue>
{
    public UnaryNotOpExpression(WorkspaceUri parentUri, ValuedExpression expression)
        : base(Tokens.LogicalNotOp, VBLongType.TypeInfo, parentUri, [expression])
    {
        Expression = expression;
    }

    public ValuedExpression Expression { get; }

    public override VBTypedValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        VBTypedValue? result = default;

        var expression = Expression.Execute(context, rethrow);
        if (expression is VBBooleanValue boolExpression)
        {
            result = new VBBooleanValue(this) { Value = !boolExpression.Value };
        }
        else if (expression is VBNumericTypedValue numericExpression)
        {
            result = numericExpression.WithValue(-1 * numericExpression.NumericValue);
        }
        else
        {
            var exception = VBRuntimeErrorException.TypeMismatch(this);
            context.AddDiagnostics(exception);
            if (rethrow)
            {
                throw exception;
            }
        }

        return result;
    }
}


public record class PowOpExpression : BinaryOperatorExpression<VBNumericTypedValue>
{
    public PowOpExpression(WorkspaceUri parentUri, ValuedExpression left, ValuedExpression right)
        : base(Tokens.PowerOp, VBDoubleType.TypeInfo, parentUri, left, right)
    {
    }

    public override VBNumericTypedValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        VBNumericTypedValue? result = default;

        var rhsValue = Right.Execute(context, rethrow);
        var lhsValue = Left.Execute(context, rethrow);

        if (lhsValue is VBNumericTypedValue lhsNumeric)
        {
            if (rhsValue is VBNumericTypedValue rhsNumeric)
            {
                // both sides are numeric, perform subtraction
                var resultValue = Math.Pow(lhsNumeric.NumericValue, rhsNumeric.NumericValue);

                // return the widest of the two types
                try
                {
                    result = (lhsNumeric.Size >= rhsNumeric.Size
                        ? lhsNumeric.WithValue(resultValue)
                        : rhsNumeric.WithValue(resultValue)) as VBNumericTypedValue;
                }
                catch (VBRuntimeErrorException vbRuntimeError)
                {
                    context.AddDiagnostics(vbRuntimeError);
                    if (rethrow)
                    {
                        throw;
                    }
                }
            }
            else
            {
                // RHS is not numeric, VBA would throw a type mismatch at runtime.
                var exception = VBRuntimeErrorException.TypeMismatch(this);
                if (rethrow)
                {
                    throw exception;
                }
            }
        }

        return result;
    }
}

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

/// <summary>
/// Represents a literal expression.
/// </summary>
public record class LiteralExpression<TValue> : ValuedExpression<TValue> where TValue : VBTypedValue
{
    public LiteralExpression(RubberduckSymbolKind kind, WorkspaceUri parentUri, TValue value)
        : base(kind, value.TypeInfo, value.ToString(), parentUri)
    {
        Value = value;
    }

    public TValue Value { get; }

    public override TValue? Execute(VBExecutionContext context, bool rethrow = false) => Value;
}

public record class StringLiteralExpression : LiteralExpression<VBStringValue>
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
public record class ObjectLiteralExpression : LiteralExpression<VBObjectValue>
{
    public ObjectLiteralExpression(WorkspaceUri parentUri)
        : base(RubberduckSymbolKind.Nothing, parentUri, VBObjectValue.Nothing)
    {
    }
}

public record class DateLiteralExpression : LiteralExpression<VBDateValue>
{
    public DateLiteralExpression(WorkspaceUri parentUri, VBDateValue value)
        : base(RubberduckSymbolKind.DateLiteral, parentUri, value)
    {
    }
}

public record class BooleanLiteralExpression : LiteralExpression<VBBooleanValue>
{
    public BooleanLiteralExpression(WorkspaceUri parentUri, VBBooleanValue value)
        : base(RubberduckSymbolKind.BooleanLiteral, parentUri, value)
    {
    }
}

public record class HexLiteralExpression : NumericLiteralExpression<VBLongValue>
{
    public HexLiteralExpression(WorkspaceUri parentUri, VBLongValue value)
        : base(parentUri, value)
    {
    }
}

public record class OctalLiteralExpression : NumericLiteralExpression<VBLongValue>
{
    public OctalLiteralExpression(WorkspaceUri parentUri, VBLongValue value)
        : base(parentUri, value)
    {
    }
}

public abstract record class VariantLiteralExpression : LiteralExpression<VBVariantValue>
{
    protected VariantLiteralExpression(WorkspaceUri parentUri, VBVariantValue value)
        : base(RubberduckSymbolKind.VariantLiteral, parentUri, value)
    {
    }
}

public record class EmptyLiteralExpression : VariantLiteralExpression
{
    public EmptyLiteralExpression(WorkspaceUri parentUri)
        : base(parentUri, VBEmptyValue.Empty.AsVariant())
    {
    }
}

public record class NullLiteralExpression : VariantLiteralExpression
{
    public NullLiteralExpression(WorkspaceUri parentUri)
        : base(parentUri, VBNullValue.Null.AsVariant())
    {
    }
}

/// <summary>
/// Represents an implicitly-typed numeric literal value.
/// </summary>
public record class NumericLiteralExpression : LiteralExpression<VBNumericTypedValue>
{
    public NumericLiteralExpression(WorkspaceUri parentUri, VBNumericTypedValue value)
        : base(RubberduckSymbolKind.NumberLiteral, parentUri, value)
    {
    }
}

public record class NumericLiteralExpression<TValue> : LiteralExpression<TValue>
    where TValue : VBNumericTypedValue
{
    public NumericLiteralExpression(WorkspaceUri parentUri, TValue value)
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
public record class ObjectValuedExpression : ValuedExpression<VBObjectValue>
{
    public ObjectValuedExpression(WorkspaceUri parentUri, IEnumerable<ValuedExpression>? children = null)
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

/// <summary>
/// Represents an expression that evaluates to a string value.
/// </summary>
public record class StringValuedExpression : ValuedExpression
{
    public StringValuedExpression(WorkspaceUri parentUri, IEnumerable<ValuedExpression>? children = null)
        : base(RubberduckSymbolKind.Expression, VBStringType.TypeInfo, string.Empty, parentUri, children)
    {
    }
}

public record class BooleanValuedExpression : ValuedExpression
{
    public BooleanValuedExpression(WorkspaceUri parentUri, IEnumerable<ValuedExpression>? children = null)
        : base(RubberduckSymbolKind.Expression, VBBooleanType.TypeInfo, string.Empty, parentUri, children)
    {
    }
}

public record class NumericValuedExpression : ValuedExpression
{
    public NumericValuedExpression(WorkspaceUri parentUri, IEnumerable<ValuedExpression>? children = null)
        : base(RubberduckSymbolKind.Expression, VBDoubleType.TypeInfo, string.Empty, parentUri, children)
    {
    }
}

public record class InvalidExpression : ValuedExpression
{
    public InvalidExpression(string name, WorkspaceUri parentUri, IEnumerable<ValuedExpression>? children = null)
        : base(RubberduckSymbolKind.UnknownSymbol, VBAnyType.TypeInfo, name, parentUri, children)
    {
    }
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
        Type = type ?? ResolveIntrinsicType(asTypeName);
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

    public VBType? Type { get; init; }
    public TypedSymbol WithResolvedType(VBType? resolvedType) => this with { Type = resolvedType };
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

    public abstract VBTypedValue Execute(VBExecutionContext context, bool rethrow = false);

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

    public VBTypedValue? Execute(VBExecutionContext context, bool rethrow = false) => context.CurrentScope.GetTypedValue(this);

    public ITypedSymbol WithResolvedValueExpressionType(VBType? resolvedValueExpressionType) => this with { ResolvedValueExpressionType = resolvedValueExpressionType };
}

public record class UserDefinedTypeSymbol : TypedSymbol
{
    public UserDefinedTypeSymbol(string name, WorkspaceUri parentUri, Accessibility accessibility, IEnumerable<UserDefinedTypeMemberSymbol> children)
        : base(RubberduckSymbolKind.UserDefinedType, accessibility, name, parentUri, children.Cast<Symbol>())
    {
        Type = new VBUserDefinedType(name, parentUri, this);
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

    public VBTypedValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        var member = context.GetModuleMember(this);
        if (member != null)
        {
            var scope = context.EnterScope(member);
            scope.Execute(context, rethrow);

        }
        throw VBRuntimeErrorException.PropertyOrMethodNotFound(this); // fitting, but is it really a VB-trappable error? or it's a .net-side bug?
    }
}

public record class ProcedureSymbol : TypedSymbol, IExecutable
{
    public ProcedureSymbol(string name, WorkspaceUri parentUri, Accessibility accessibility, IEnumerable<Symbol>? children = null, RubberduckSymbolKind kind = RubberduckSymbolKind.Procedure)
        : base(kind, accessibility, name, parentUri, (children ?? []).ToArray(), VBLongPtrType.TypeInfo) { }

    public VBTypedValue? Evaluate(VBExecutionScope context, bool rethrow = false) =>
        context.GetTypedValue(this) as VBLongPtrValue; // symbol table contains a VBLongPtrValue for procedures that can be used with the AddressOf operator.

    public VBTypedValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        var member = context.GetModuleMember(this);
        if (member != null)
        {
            try
            {
                var scope = context.EnterScope(member);
                scope.Execute(context);
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
        Type = new VBEnumType(name, parentUri, this, isUserDefined: isUserDefined);
    }
}

public record class EnumMemberSymbol : ValuedTypedSymbol
{
    public EnumMemberSymbol(string name, WorkspaceUri parentUri, string? value)
        : base(RubberduckSymbolKind.EnumMember, Accessibility.Public, name, parentUri, null, value)
    {
        Type = VBLongType.TypeInfo;
    }

    public override VBTypedValue Execute(VBExecutionContext context, bool rethrow = false) => context.CurrentScope.GetTypedValue(this);
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

    public override VBTypedValue Execute(VBExecutionContext context, bool rethrow = false) => context.CurrentScope.GetTypedValue(this);
}

public record class VariableDeclarationSymbol : DeclarationExpressionSymbol
{
    public VariableDeclarationSymbol(string name, WorkspaceUri parentUri, Accessibility accessibility, VBType type)
        : base(RubberduckSymbolKind.Variable, name, parentUri, accessibility, children: [], annotations: [], type: type) { }
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

public abstract record class OperatorSymbol : ValuedExpression<VBTypedValue>
{
    public OperatorSymbol(string token, VBType type, WorkspaceUri parentUri, IEnumerable<ValuedExpression>? children)
        : base(RubberduckSymbolKind.Operator, type, token, parentUri, children)
    {
    }

    public override VBTypedValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        try
        {
            return EvaluateResult(context);
        }
        catch (VBCompileErrorException vbCompileError)
        {
            foreach (var diagnostic in vbCompileError.Diagnostics)
            {
                context.AddDiagnostic(diagnostic);
            }

            if (rethrow)
            {
                throw;
            }

            return null;
        }
        catch (VBRuntimeErrorException vbRuntimeError)
        {
            foreach (var diagnostic in vbRuntimeError.Diagnostics)
            {
                context.AddDiagnostic(diagnostic);
            }

            if (rethrow)
            {
                throw;
            }

            return null;
        }
    }

    protected abstract VBTypedValue? EvaluateResult(VBExecutionContext context);
}

public record class LineLabelSymbol : Symbol
{
    public LineLabelSymbol(string name, WorkspaceUri parentUri)
        : base(RubberduckSymbolKind.LineLabel, name, parentUri)
    {
    }
    public bool IsLineNumber => int.TryParse(Name, out _);
}

/// <summary>
/// Represents an executable VBA statement.
/// </summary>
public abstract record class ExecutableStatement : IExecutable
{
    protected ExecutableStatement(WorkspaceUri parentUri, LineLabelSymbol? parentLabel = default)
    {
        ParentUri = parentUri;
        ParentLabel = parentLabel;
    }

    /// <summary>
    /// The URI of the parent symbol.
    /// </summary>
    /// <remarks>
    /// This information should not be used to construct a symbol hierarchy.
    /// </remarks>
    public WorkspaceUri ParentUri { get; init; }

    /// <summary>
    /// The line label (or number) associated with this statement, if any.
    /// </summary>
    public LineLabelSymbol? ParentLabel { get; init; }

    /// <summary>
    /// Executes the statement in the given context.
    /// </summary>
    public VBTypedValue? Execute(VBExecutionContext context, bool rethrow = false) => ExecuteInternal(context, rethrow);

    protected virtual VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false) => default;
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

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        VBTypedValue? result = default;
        try
        {
            if (Target is VBTypeMember target)
            {
                context.EnterScope(target);
            }
            result = Target.Execute(context, rethrow);
        }
        catch (VBRuntimeErrorException exception)
        {
            context.AddDiagnostics(exception);
            if (rethrow)
            {
                throw;
            }
        }
        catch (VBCompileErrorException exception)
        {
            context.AddDiagnostics(exception);
            if (rethrow)
            {
                throw;
            }
        }

        return result;
    }
}

/// <summary>
/// Represents a block statement, that can contain child executable statements.
/// </summary>
public abstract record class BlockStatement : ExecutableStatement
{
    public BlockStatement(WorkspaceUri parentUri, IEnumerable<IExecutable>? children = null, LineLabelSymbol? parentLabel = null)
        : base(parentUri, parentLabel)
    {
        Children = children ?? [];
    }

    public IEnumerable<IExecutable> Children { get; }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        var success = ExecuteBody(context, rethrow);
        return new VBBooleanValue().WithValue(success);
    }

    protected bool ExecuteBody(VBExecutionContext context, bool rethrow = false)
    {
        try
        {
            // TODO implement execution pointer and orchestrate execution in the context
            foreach (var child in Children)
            {
                _ = child.Execute(context, rethrow);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public record class GoToStatement : ExecutableStatement
{
    public GoToStatement(WorkspaceUri parentUri, LineLabelSymbol targetLabel, bool isImplicit = false)
        : base(parentUri)
    {
        TargetLabel = targetLabel;
        IsImplicit = isImplicit;
    }

    public bool IsImplicit { get; init; }
    public LineLabelSymbol TargetLabel { get; }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        // TODO this will be easier with context.ExecutionPointer
        return base.ExecuteInternal(context, rethrow);
    }
}

public record class IfStatement : BlockStatement
{
    public IfStatement(WorkspaceUri parentUri, BooleanValuedExpression condition, IEnumerable<IExecutable> body, IEnumerable<ElseIfStatement> elseIfBlocks = default, ElseStatement? elseBlock = default)
        : base(parentUri, body)
    {
        Condition = condition;
        ElseIfBlocks = elseIfBlocks ?? [];
        ElseBlock = elseBlock;
    }

    public BooleanValuedExpression Condition { get; }

    public IEnumerable<ElseIfStatement> ElseIfBlocks { get; } = [];
    public ElseStatement? ElseBlock { get; init; }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        var condition = Condition.Execute(context, rethrow);
        if (condition is VBBooleanValue conditionResult)
        {
            if (conditionResult.Value)
            {
                ExecuteBody(context, rethrow);
                return VBBooleanValue.True;
            }
            else
            {
                foreach (var elseIfBlock in ElseIfBlocks)
                {
                    if (elseIfBlock.Execute(context, rethrow) is VBBooleanValue didExecute && didExecute.Value)
                    {
                        break;
                    }
                }

                ElseBlock?.Execute(context, rethrow);
                return VBBooleanValue.False;
            }
        }

        return default;
    }
}

public record class ElseIfStatement : BlockStatement
{
    public ElseIfStatement(WorkspaceUri parentUri, BooleanValuedExpression condition, IEnumerable<IExecutable> body)
        : base(parentUri, body)
    {
        Condition = condition;
    }

    public BooleanValuedExpression Condition { get; }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        var condition = Condition.Execute(context, rethrow);
        if (condition is VBBooleanValue conditionResult)
        {
            if (conditionResult.Value)
            {
                ExecuteBody(context, rethrow);
                return VBBooleanValue.True;
            }
            else
            {
                return VBBooleanValue.False;
            }
        }

        return default;
    }
}

public record class ElseStatement : BlockStatement
{
    public ElseStatement(WorkspaceUri parentUri, IEnumerable<IExecutable> body)
        : base(parentUri, body) { }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        ExecuteBody(context, rethrow);
        return default;
    }
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

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        var didEnter = false;
        var scope = context.CurrentScope;
        switch (LoopType)
        {
            case DoLoopType.DoLoop:
                didEnter = true;
                ExecuteBody(context, rethrow);
                break; // unconditioned loop, execute body once

            case DoLoopType.DoUntil:
            case DoLoopType.DoWhile:
                while (Condition?.Execute(context, rethrow) is VBBooleanValue vbBool && !vbBool.Value)
                {
                    didEnter = true;
                    ExecuteBody(context, rethrow);
                    break; // only execute once for now
                }
                break;

            case DoLoopType.LoopUntil:
            case DoLoopType.LoopWhile:
                do
                {
                    didEnter = true;
                    ExecuteBody(context, rethrow);
                    break; // only execute once for now
                }
                while (Condition?.Execute(context, rethrow) is VBBooleanValue vbBool && vbBool.Value);
                break;

            default:
                break;
        }

        return new VBBooleanValue() { Value = didEnter };
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
}

public record class WithStatementSymbol : BlockStatement
{
    public WithStatementSymbol(ObjectValuedExpression targetExpression, WorkspaceUri parentUri, IEnumerable<IExecutable>? children = null)
        : base(parentUri, children)
    {
        TargetExpression = targetExpression;
    }

    public ObjectValuedExpression TargetExpression { get; init; }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        var withObjectVariable = TargetExpression.Execute(context, rethrow);
        if (withObjectVariable == VBObjectValue.Nothing)
        {
            context.AddDiagnostics(VBRuntimeErrorException.ObjectVariableNotSet(TargetExpression, "With block variable or expression evaluates to Nothing"));
        }

        ExecuteBody(context, rethrow);
        return default;
    }
}

public record class ExitStatement : ExecutableStatement
{
    public ExitStatement(WorkspaceUri parentUri, BlockStatement? binding, LineLabelSymbol? parentLabel = null)
        : base(parentUri, parentLabel)
    {
        Binding = binding;
    }

    public BlockStatement? Binding { get; }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        if (Binding is not null)
        {
            context.ExitBlock(Binding);
        }

        return default;
    }
}

public record class RaiseEventStatement : ExecutableStatement
{
    public RaiseEventStatement(WorkspaceUri parentUri, LineLabelSymbol? parentLabel = null)
        : base(parentUri, parentLabel)
    {
    }
}

public record class DimStatement : ExecutableStatement
{
    // NOTE: Dim statements are executed upon entering a scope

    public DimStatement(WorkspaceUri parentUri, IEnumerable<TypedSymbol> symbols, LineLabelSymbol? parentLabel = null)
        : base(parentUri, parentLabel)
    {
        Symbols = symbols;
    }

    public IEnumerable<TypedSymbol> Symbols { get; }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        foreach (var symbol in Symbols)
        {
            context.AddSymbol(symbol);
        }

        return default;
    }
}

public record class ReDimStatement : DimStatement
{
    public ReDimStatement(WorkspaceUri parentUri, IEnumerable<TypedSymbol> symbols, bool preserve = false, LineLabelSymbol? parentLabel = null)
        : base(parentUri, symbols, parentLabel)
    {
        Preserve = preserve;
    }

    public bool Preserve { get; init; }

    // NOTE: ReDim statements are declarative on scope entry, but still executable

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        foreach (var symbol in Symbols)
        {
            // TODO validate, issue diagnostics
            context.AddSymbol(symbol);
        }

        return default;
    }
}

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

public record class LetStatement : ExecutableStatement
{
    public LetStatement(WorkspaceUri parentUri, TypedSymbol target, ValuedExpression expression, LineLabelSymbol? parentLabel = null)
        : base(parentUri, parentLabel)
    {
        Target = target;
        Expression = expression;
    }

    public TypedSymbol Target { get; init; }
    public ValuedExpression Expression { get; init; }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        if (Expression.Execute(context, rethrow) is VBTypedValue value)
        {
            context.SetSymbolValue(Target, value);
        }

        return default;
    }
}

public record class EndStatement : ExecutableStatement
{
    public EndStatement(WorkspaceUri parentUri, LineLabelSymbol? parentLabel = null)
        : base(parentUri, parentLabel)
    {
    }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        // diagnose?
        return base.ExecuteInternal(context, rethrow);
    }
}

public record class StopStatement : ExecutableStatement
{
    public StopStatement(WorkspaceUri parentUri, LineLabelSymbol? parentLabel = null)
        : base(parentUri, parentLabel)
    {
    }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        // diagnose?
        return base.ExecuteInternal(context, rethrow);
    }
}

public record class OnErrorResumeNextStatement : ExecutableStatement
{
    public OnErrorResumeNextStatement(WorkspaceUri parentUri, LineLabelSymbol? parentLabel = null)
        : base(parentUri, parentLabel)
    {
    }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        context.CurrentScope.ActiveOnErrorResumeNext = true;
        return default;
    }
}

public record class OnErrorGoToStatement : ExecutableStatement
{
    public OnErrorGoToStatement(WorkspaceUri parentUri, LineLabelSymbol target, LineLabelSymbol? parentLabel = null)
        : base(parentUri, parentLabel)
    {
        Target = target;
    }

    public LineLabelSymbol Target { get; init; }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        if (context.CurrentScope.ActiveErrorState)
        {
            // TODO diagnose bad error handling
        }

        context.CurrentScope.ActiveOnErrorResumeNext = false;
        context.CurrentScope.ActiveOnErrorGoTo = Target;
        return default;
    }
}

public record class ResumeStatement : ExecutableStatement
{
    public ResumeStatement(WorkspaceUri parentUri, LineLabelSymbol target, LineLabelSymbol? parentLabel = null)
        : base(parentUri, parentLabel)
    {
        Target = target;
    }

    public LineLabelSymbol Target { get; init; }

    protected override VBTypedValue? ExecuteInternal(VBExecutionContext context, bool rethrow = false)
    {
        if (!context.CurrentScope.ActiveErrorState)
        {
            var exception = VBRuntimeErrorException.ResumeWithoutError(Target);
            context.AddDiagnostics(exception);
            if (rethrow)
            {
                throw exception;
            }
        }

        //context.CurrentScope.JumpTo(Target);
        return default;
    }
}

