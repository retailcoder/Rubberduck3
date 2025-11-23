using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Rubberduck.InternalApi.Model;
using Rubberduck.InternalApi.Model.Symbols.Abstract;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Rubberduck.InternalApi.Execution;

public enum VBCompileErrorId
{
    ForbiddenWithOptionStrict = 9000,
    InvalidUseOfObject,
    InvalidParamArrayUse,
    InvalidReDim,
    ExpectedArray,
    ExpectedIdentifier,
    LabelNotDefined,
    TypeMismatch,
    UserDefinedTypeNotDefined,
    ExitDoNotWithinDoLoop,
    ExitForNotWithinForNext,
    ExitFunctionNotAllowedInSubOrProperty,
    ExitPropertyNotAllowedInSubOrFunction,
}

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class VBCompileErrorException : ApplicationException, IDiagnosticSource
{
    private string DebuggerDisplay => $"{Message}{(Verbose is null ? string.Empty : " | " + Verbose)}";

    #region Classic-VB compile-time errors
    // NOTE: VB compile errors are just messages, ID is made up.
    public static VBCompileErrorException OptionStrictForbidden(Symbol symbol, string? verbose = null) => new(symbol, VBCompileErrorId.ForbiddenWithOptionStrict, "Option Strict forbidden implicit narrowing conversion or late-bound call.", verbose);
    public static VBCompileErrorException InvalidUseOfObject(Symbol symbol, string? verbose = null) => new(symbol, VBCompileErrorId.InvalidUseOfObject, "Invalid use of object", verbose);
    public static VBCompileErrorException InvalidParamArrayUse(Symbol symbol, string? verbose = null) => new(symbol, VBCompileErrorId.InvalidParamArrayUse, "Invalid ParamArray use", verbose);
    public static VBCompileErrorException InvalidReDim(Symbol symbol, string? verbose = null) => new(symbol, VBCompileErrorId.InvalidReDim, "Invalid ReDim", verbose);
    public static VBCompileErrorException ExpectedArray(Symbol symbol, string? verbose = null) => new(symbol, VBCompileErrorId.ExpectedArray, "Expected array", verbose);
    public static VBCompileErrorException ExpectedIdentifier(Symbol symbol, string? verbose = null) => new(symbol, VBCompileErrorId.ExpectedIdentifier, "Expected identifier", verbose);
    public static VBCompileErrorException LabelNotDefined(Symbol symbol, string? verbose = null) => new(symbol, VBCompileErrorId.LabelNotDefined, "Label not defined", verbose);
    public static VBCompileErrorException TypeMismatch(Symbol symbol, string? verbose = null) => new(symbol, VBCompileErrorId.TypeMismatch, "Type mismatch", verbose);
    public static VBCompileErrorException UserDefinedTypeNotDefined(Symbol symbol, string? verbose = null) => new(symbol, VBCompileErrorId.UserDefinedTypeNotDefined, "User-defined type not defined", verbose);
    public static VBCompileErrorException ExitDoNotWithinDoLoop(Symbol symbol, string? verbose = null) => new(symbol, VBCompileErrorId.ExitDoNotWithinDoLoop, "Exit Do not within Do...Loop", verbose);
    public static VBCompileErrorException ExitForNotWithinForNext(Symbol symbol, string? verbose = null) => new(symbol, VBCompileErrorId.ExitForNotWithinForNext, "Exit For not within For...Next", verbose);
    public static VBCompileErrorException ExitFunctionNotAllowedInSubOrProperty(Symbol symbol, string? verbose = null) => new(symbol, VBCompileErrorId.ExitFunctionNotAllowedInSubOrProperty, "Exit Function not allowed in Sub or Property", verbose);
    #endregion

    public VBCompileErrorException(Symbol symbol, VBCompileErrorId id, string message, string? verbose = null)
        : base($"Compile error: {message}")
    {
        VBCompileErrorId = id;
        Symbol = symbol;
        Verbose = verbose;
    }

    public string DiagnosticCode => $"VBC{(int)VBCompileErrorId:00000}";
    public VBCompileErrorId VBCompileErrorId { get; }
    public Symbol Symbol { get; }
    public string? Verbose { get; }

    public Diagnostic Diagnostic => RubberduckDiagnostic.CompileError(this);
    public IEnumerable<Diagnostic> Diagnostics => [Diagnostic];
}
