using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Model;
using Rubberduck.InternalApi.Model.Declarations.Types;
using Rubberduck.InternalApi.Model.Declarations.Types.Abstract;
using Rubberduck.InternalApi.Model.Symbols;
using Rubberduck.InternalApi.Model.Symbols.Abstract;
using Rubberduck.InternalApi.Model.Symbols.Declarations;
using Rubberduck.InternalApi.Model.Symbols.Statements;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Rubberduck.InternalApi.Execution;

public enum VBOptionCompare
{
    Binary,
    Text,
    Database
}

public record class VBExecutionScope : IExecutable, IDisposable
{
    private Dictionary<TypedSymbol, VBTypedValue> _symbols = [];
    private readonly ImmutableArray<IExecutable> _instructions;

    public VBExecutionScope(VBExecutionContext context, VBTypeMember member, VBRuntimeErrorException? error = null)
    {
        ExecutionContext = context;
        MemberInfo = member;
        Error = error;

        SetOptionsFromModuleParent(context, member);

        _instructions = [.. member.Symbol?.Children?.OfType<IExecutable>() ?? []];
    }

    private void SetOptionsFromModuleParent(VBExecutionContext context, VBTypeMember member)
    {
        var parentUri = member.Symbol!.ParentUri;
        if (context.GetSymbol(parentUri) is TypedSymbol symbol)
        {
            if (symbol is ClassModuleSymbol classModule)
            {
                OptionExplicit = classModule.OptionExplicit;
                OptionBase = classModule.OptionBase ?? 0;
                OptionCompare = classModule.OptionCompare ?? VBOptionCompare.Binary;
            }
        }
    }

    public VBExecutionContext ExecutionContext { get; }

    public bool OptionStrict { get; private set; }
    public bool OptionExplicit { get; private set; }
    public int OptionBase { get; private set; }
    public VBOptionCompare OptionCompare { get; private set; }

    public VBTypedValue GetTypedValue(TypedSymbol symbol) => _symbols[symbol];
    public void SetTypedValue(TypedSymbol symbol, VBTypedValue value) => _symbols[symbol] = value;

    public VBTypeMember MemberInfo { get; init; }

    public VBRuntimeErrorException? Error { get; set; }
    public bool ActiveOnErrorResumeNext { get; set; }
    public LineLabelSymbol? ActiveOnErrorGoTo { get; set; }
    public bool ActiveErrorState => Error != null;

    private int ActiveInstruction { get; set; }

    private bool TryFindLabelTarget(LineLabelSymbol target, out int instruction)
    {
        var targetInstruction = _instructions.OfType<ExecutableStatement>()
            .Select((instruction, index) => new { instruction, index })
            .FirstOrDefault(e => e.instruction.ParentLabel == target)?.index;

        if (targetInstruction.HasValue)
        {
            instruction = targetInstruction.Value;
            return true;
        }
        else
        {
            instruction = default;
            return false;
        }
    }

    public bool TryGoTo(LineLabelSymbol target)
    {
        if (TryFindLabelTarget(target, out var instruction))
        {
            ActiveInstruction = instruction;
            return true;
        }

        return false;
    }

    private Stack<int> _returnTo { get; } = [];
    public bool TryGoSub(LineLabelSymbol target)
    {
        if (TryFindLabelTarget(target, out var instruction))
        {
            _returnTo.Push(ActiveInstruction);
            ActiveInstruction = instruction;
            return true;
        }

        return false;
    }

    public bool TryReturn()
    {
        if (_returnTo.TryPop(out var instruction))
        {
            ActiveInstruction = instruction;
            return true;
        }
        return false;
    }

    private int? _resumeTo;
    public bool TryResume()
    {
        if (!_resumeTo.HasValue)
        {
            _resumeTo = null;
            return false;
        }

        ActiveInstruction = _resumeTo.Value;
        Error = null;
        _resumeTo = null;
        return true;
    }

    public bool TryResume(LineLabelSymbol target)
    {
        if (TryFindLabelTarget(target, out var instruction))
        {
            _resumeTo = null;
            ActiveInstruction = instruction;
            return true;
        }

        _resumeTo = null;
        return false;
    }


    private Stack<int> _exitTo = [];
    public void EnterBlock()
    {
        _exitTo.Push(ActiveInstruction);
    }
    public bool TryExitBlock(BlockStatement binding)
    {
        if (_exitTo.TryPop(out var instruction))
        {
            ActiveInstruction = instruction;
            return true;
        }
        return false;
    }

    private Stack<VBObjectValue> _withBlockObject = [];
    public bool TryEnterWithBlock(VBObjectValue reference)
    {
        if (reference.IsNothing())
        {
            return false;
        }

        _withBlockObject.Push(reference);
        EnterBlock();

        return true;
    }
    public bool TryExitWithBlock()
    {
        if (_withBlockObject.TryPop(out var reference))
        {
            if (reference.Symbol is not VariableDeclarationSymbol && reference.Symbol != null)
            {
                // the reference was held by the With block
                SetTypedValue(reference.Symbol, VBObjectValue.Nothing);
            }
            return true;
        }
        return false;
    }


    private bool _isEnding;
    public void End()
    {
        Error = null;

        _withBlockObject.Clear();
        _returnTo.Clear();
        _exitTo.Clear();
        _resumeTo = null;

        _isEnding = true;
    }

    public VBTypedValue? Execute(VBExecutionContext context, bool rethrow = false)
    {
        VariableDeclarationSymbol? implicitReturnValue = default;
        if (MemberInfo.Symbol is FunctionSymbol function)
        {
            var returnType = function.Type ?? VBVariantType.TypeInfo;
            implicitReturnValue = new VariableDeclarationSymbol(function.Name, function.Uri, Accessibility.Implicit, returnType);

            _symbols.Add(implicitReturnValue, returnType.DefaultValue);
        }

        for (var i = 0; i < _instructions.Length; i++)
        {

            var instruction = _instructions[i];
            ActiveInstruction = i;

            try
            {
                _ = instruction.Execute(context, rethrow);

                if (_isEnding)
                {
                    context.End();
                    break;
                }

                // handle jumps (goto, gosub, exit, etc.)
                if (ActiveInstruction != i)
                {
                    i = ActiveInstruction - 1; // offsetting the next i++
                    continue;
                }
            }
            catch (VBRuntimeErrorException vbRuntimeError)
            {
                Error = vbRuntimeError;
                if (ActiveOnErrorGoTo != null)
                {
                    if (TryFindLabelTarget(ActiveOnErrorGoTo, out var errHandler))
                    {
                        i = errHandler - 1; // offsetting the next i++
                        continue;
                    }
                    else
                    {
                        Debug.Assert(false); // something has gone very wrong
                    }
                }

                // error is unhandled
                context.AddDiagnostics(vbRuntimeError); // needs an "unhandled error" diagnostic?
                if (rethrow)
                {
                    throw;
                }
            }
            catch (VBCompileErrorException vbCompileError)
            {
                context.AddDiagnostic(RubberduckDiagnostic.CompileError(vbCompileError));
                if (rethrow)
                {
                    throw;
                }
            }
        }

        if (!_isEnding)
        {
            context.ExitScope();
            if (implicitReturnValue != default)
            {
                _symbols.Remove(implicitReturnValue);
            }
        }
        return default;
    }

    public void Dispose()
    {
        _symbols.Clear();
    }
}
