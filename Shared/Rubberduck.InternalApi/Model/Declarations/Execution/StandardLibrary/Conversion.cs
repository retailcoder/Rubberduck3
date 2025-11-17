using Rubberduck.InternalApi.Model.Declarations.Execution.Values;
using Rubberduck.InternalApi.Model.Declarations.Operators;
using Rubberduck.InternalApi.Model.Declarations.Symbols;
using Rubberduck.InternalApi.Model.Declarations.Types;
using Rubberduck.InternalApi.Model.Declarations.Types.Abstract;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using System;

namespace Rubberduck.InternalApi.Model.Declarations.Execution.StandardLibrary
{
    public static class Conversion
    {
        public static VBBooleanValue CBool(VBExecutionContext context, TypedSymbol symbol, VBTypedValue value) =>
            CheckNullError(value, e => new VBBooleanValue(symbol).WithValue(e.Value != 0), out var nop)
                ? nop : SymbolOperation.ExecuteCompareOpResult(context, symbol, VBIntegerValue.Zero, value, (lhs, rhs) => lhs != rhs);

        public static VBByteValue CByte(VBExecutionContext context, TypedSymbol symbol, VBTypedValue value) =>
            CheckNullError(value, e => new VBByteValue(symbol).WithValue(e.Value), out var nop)
                ? nop : (VBByteValue)(new VBByteValue(symbol)).WithValue(GetNumericValueOrThrow(context, value));

        public static VBCurrencyValue CCur(VBExecutionContext context, TypedSymbol symbol, VBTypedValue value) =>
            CheckNullError(value, e => (VBCurrencyValue)new VBCurrencyValue(symbol).WithValue(e.Value), out var nop)
                ? nop : (VBCurrencyValue)new VBCurrencyValue(symbol).WithValue(GetNumericValueOrThrow(context, value));

        public static VBDateValue CDate(VBExecutionContext context, VBTypedValue value)
        {
            if (CheckNullError(value, null as Func<VBTypedValue, VBDateValue>, out var nop))
            {
                // already a date
                return nop;
            }

            if (TryConvertNumericValue(context, value, out var numeric))
            {
                // from serial
                return (VBDateValue)new VBDateValue(value.Symbol!).WithValue(numeric);
            }

            // this is probably excluding a bunch of weird valid date literals
            if (DateTime.TryParse(GetStringValueOrThrow(value), out var dtValue))
            {
                return new VBDateValue(value.Symbol!).WithValue(dtValue);
            }

            throw VBRuntimeErrorException.TypeMismatch(value.Symbol!.Range, $"Type `{value.TypeInfo.Name}` cannot be converted directly to a `Date`.");
        }

        public static VBDoubleValue CDbl(VBExecutionContext context, TypedSymbol symbol, VBTypedValue value) =>
            CheckNullError(value, e => (VBDoubleValue)new VBDoubleValue(symbol).WithValue(e.Value), out var nop)
                ? nop : (VBDoubleValue)new VBDoubleValue(symbol).WithValue(GetNumericValueOrThrow(context, value));

        public static VBDecimalValue CDec(VBExecutionContext context, TypedSymbol symbol, VBTypedValue value) =>
            CheckNullError(value, e => new VBDecimalValue(symbol).WithValue(e.Value), out var nop)
                ? nop : (VBDecimalValue)new VBDecimalValue(symbol).WithValue(GetNumericValueOrThrow(context, value));

        public static VBIntegerValue CInt(VBExecutionContext context, TypedSymbol symbol, VBTypedValue value) =>
            CheckNullError(value, e => (VBIntegerValue)new VBIntegerValue(symbol).WithValue(e.Value), out var nop)
                ? nop : (VBIntegerValue)new VBIntegerValue(symbol).WithValue(GetNumericValueOrThrow(context, value));

        public static VBLongValue CLng(VBExecutionContext context, TypedSymbol symbol, VBTypedValue value) =>
            CheckNullError(value, e => (VBLongValue)new VBLongValue(symbol).WithValue(e.Value), out var nop)
                ? nop : (VBLongValue)new VBLongValue(symbol).WithValue(GetNumericValueOrThrow(context, value));

        public static VBLongLongValue CLngLng(VBExecutionContext context, TypedSymbol symbol, VBTypedValue value) =>
            CheckNullError(value, e => (VBLongLongValue)new VBLongLongValue(symbol).WithValue(e.Value), out var nop)
                ? nop : (VBLongLongValue)new VBLongLongValue(symbol).WithValue(GetNumericValueOrThrow(context, value));

        public static VBLongPtrValue CLngPtr(VBExecutionContext context, TypedSymbol symbol, VBTypedValue value)
        {
            VBType ptrSize = context.Is64BitHost ? VBLongLongType.TypeInfo : VBLongType.TypeInfo;
            return CheckNullError(value, e => (VBLongPtrValue)new VBLongPtrValue(symbol).WithValue(e.Value, ptrSize), out var nop)
                ? nop : (VBLongPtrValue)new VBLongPtrValue(symbol).WithValue(GetNumericValueOrThrow(context, value), ptrSize);
        }

        public static VBSingleValue CSng(VBExecutionContext context, TypedSymbol symbol, VBTypedValue value) =>
            CheckNullError(value, e => (VBSingleValue)new VBSingleValue(symbol).WithValue(e.Value), out var nop)
                ? nop : (VBSingleValue)new VBSingleValue(symbol).WithValue(GetNumericValueOrThrow(context, value));

        public static VBStringValue CStr(VBExecutionContext context, TypedSymbol symbol, VBTypedValue value) =>
            CheckNullError(value, null as Func<VBTypedValue, VBStringValue>, out var nop)
                ? nop : (VBStringValue)new VBStringValue(symbol).WithValue(GetStringValueOrThrow(value));

        public static VBVariantValue CVar(VBTypedValue value) => value is VBVariantValue nop ? nop : new VBVariantValue(value, value.Symbol!);
        public static VBVariantValue CVDate(VBExecutionContext context, TypedSymbol symbol, VBTypedValue value) => new(CDate(context, value), symbol!);


        public static VBVariantValue Error(VBExecutionContext context, TypedSymbol symbol, TypedSymbol? lhs, VBTypedValue value)
        {
            var number = (int)GetNumericValueOrThrow(context, value);
            var message = VBRuntimeErrorException.GetErrorString(number);

            if (lhs is null) // function is being used as a statement
            {
                context.AddDiagnostic(RubberduckDiagnostic.PreferErrRaiseOverErrorStatement(symbol));
                if (number == 0)
                {
                    throw VBRuntimeErrorException.InvalidProcedureCallOrArgument(symbol,
                        "Error code 0 encodes the \"no error\" state; the `Error` statement cannot raise error 0, so the argument is invalid.");
                }

                // throwing the user-intended runtime error here
                throw new VBRuntimeErrorException(symbol, number, message);
            }

            return new VBVariantValue(new VBStringValue(symbol).WithValue(message), symbol);
        }

        public static VBStringValue ErrorS(VBExecutionContext context, TypedSymbol symbol, VBTypedValue value) =>
            new VBStringValue(symbol).WithValue(VBRuntimeErrorException.GetErrorString((int)GetNumericValueOrThrow(context, value)));

        public static VBVariantValue CVErr(VBExecutionContext context, TypedSymbol symbol, VBTypedValue value) =>
            Convert(context, symbol, value, n => new VBErrorValue(symbol, (int)n));
        public static VBVariantValue Fix(VBExecutionContext context, TypedSymbol symbol, VBTypedValue value) =>
            Convert(context, symbol, value, n => Math.Sign(n) * Math.Truncate(Math.Abs(n)));
        public static VBVariantValue Hex(VBExecutionContext context, TypedSymbol symbol, VBTypedValue value) =>
            Convert(context, symbol, value, n => n.ToString("X"));
        public static VBStringValue HexS(VBExecutionContext context, TypedSymbol symbol, VBTypedValue value) =>
            ConvertNumericString(context, symbol, value, n => n.ToString("X"));
        public static VBVariantValue Int(VBExecutionContext context, TypedSymbol symbol, VBTypedValue value) =>
            Convert(context, symbol, value, n => Math.Truncate(n));
        public static VBVariantValue Oct(VBExecutionContext context, TypedSymbol symbol, VBTypedValue value) =>
            Convert(context, symbol, value, n => System.Convert.ToString((long)n, toBase: 8));
        public static VBStringValue OctS(VBExecutionContext context, TypedSymbol symbol, VBTypedValue value) =>
            ConvertNumericString(context, symbol, value, n => System.Convert.ToString((long)n, toBase: 8));
        public static VBVariantValue Str(VBExecutionContext context, TypedSymbol symbol, VBTypedValue value) =>
            new VBVariantValue(new VBStringValue(symbol).WithValue(GetStringValueOrThrow(value)), symbol);
        public static VBStringValue StrS(VBExecutionContext context, TypedSymbol symbol, VBTypedValue value) =>
            new VBStringValue(symbol).WithValue(GetStringValueOrThrow(value));

        public static VBDoubleValue Val(VBExecutionContext context, TypedSymbol symbol, VBTypedValue value) =>
            CheckNullError(value, null as Func<VBTypedValue, VBDoubleValue>, out var nop)
                ? nop : throw new NotImplementedException(); // <~ TODO!

        private static VBVariantValue Convert<T>(VBExecutionContext context, TypedSymbol symbol, VBTypedValue value, Func<double, T> op) =>
            new VBVariantValue(value, symbol) { Value = value is VBNullValue ? value : op(GetNumericValueOrThrow(context, value)) };

        private static VBStringValue ConvertNumericString(VBExecutionContext context, TypedSymbol symbol, VBTypedValue value, Func<double, string> op) =>
            new VBStringValue(symbol).WithValue(op(GetNumericValueOrThrow(context, value)));

        /// <summary>
        /// Throws a <c>VBRuntimeErrorException.TypeMismatch</c> if it isn't a string, or can't be coerced into one.
        /// </summary>
        /// <exception cref="VBRuntimeErrorException" />
        private static string GetStringValueOrThrow(VBTypedValue value) => value is IStringCoercion coercible
            ? coercible.AsCoercedString()?.Value ?? throw VBRuntimeErrorException.TypeMismatch(value.Symbol!.Range)
            : throw VBRuntimeErrorException.TypeMismatch(value.Symbol!.Range);

        /// <summary>
        /// Throws a <c>VBRuntimeErrorException.TypeMismatch</c> if it isn't a number, or can't be coerced into one.
        /// </summary>
        /// <exception cref="VBRuntimeErrorException" />
        private static double GetNumericValueOrThrow(VBExecutionContext context, VBTypedValue value) =>
            TryConvertNumericValue(context, value, out var numericResult)
                ? numericResult
                : throw VBRuntimeErrorException.TypeMismatch(value.Symbol!.Range);

        private static bool TryConvertNumericValue(VBExecutionContext context, VBTypedValue value, out double numericResult)
        {
            if (value is INumericValue numeric)
            {
                numericResult = numeric.AsDouble().Value;
                return true;
            }

            if (value is INumericCoercion coercible)
            {
                var coerced = coercible.AsCoercedNumeric();
                context.AddDiagnostic(RubberduckDiagnostic.ImplicitNumericCoercion(value.Symbol!));

                if (coerced != null)
                {
                    numericResult = coerced.Value;
                    return true;
                }
            }

            numericResult = VBDoubleValue.Zero.Value;
            return false;
        }

        /// <summary>
        /// Validates the value against <c>VBNullValue</c> and <c>VBErrorValue</c>.
        /// </summary>
        /// <remarks>
        /// <c>VBRuntimeErrorException.TypeMismatch</c> is thrown when the <c>value</c> is a <c>VBErrorValue</c> and cannot be converted;  
        /// <c>VBRuntimeErrorException.InvalidUseOfNull</c> is thrown whenever the <c>value</c> is a <c>VBNullValue</c>.
        /// </remarks>
        /// <typeparam name="T">The target VBType</typeparam>
        /// <param name="value">The typed value to validate</param>
        /// <param name="convertErrorValue">A function to convert <c>VBErrorValue</c> values. If <c>null</c>, a <c>VBRuntimeException.TypeMismatch</c> is added to the execution context.</param>
        /// <param name="typedResult">The typed result, if successfully converted from an <c>VBErrorValue</c> or if the value was already of the correct type.</param>
        /// <returns>
        /// <c>true</c> if a typed result was successfully converted (or if the result is excatly the value that was given), <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="VBRuntimeErrorException" />
        private static bool CheckNullError<T>(VBTypedValue value, Func<VBErrorValue, T>? convertErrorValue, out T typedResult) where T : VBTypedValue
        {
            if (value is T nop)
            {
                typedResult = nop;
                return true;
            }

            if (value is VBErrorValue error)
            {
                if (convertErrorValue is null)
                {
                    throw VBRuntimeErrorException.TypeMismatch(value.Symbol!.Range, $"Type `{value.TypeInfo.Name}` cannot be converted to {typeof(T).Name}.");
                }

                typedResult = convertErrorValue(error);
                return true;
            }

            if (value is VBNullValue)
            {
                throw VBRuntimeErrorException.InvalidUseOfNull(value.Symbol!, "Cannot convert from `Null`.");
            }

            if ((RubberduckSymbolKind)value.Symbol!.Kind == RubberduckSymbolKind.Nothing)
            {
                throw VBCompileErrorException.InvalidUseOfObject(value.Symbol, "The ");
            }

            typedResult = null!;
            return false;
        }
    }
}
