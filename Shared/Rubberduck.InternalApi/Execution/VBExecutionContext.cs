using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Rubberduck.InternalApi.Execution.Values;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Types.Abstract;
using Rubberduck.InternalApi.Model.Symbols;
using Rubberduck.InternalApi.Model.Symbols.Abstract;
using Rubberduck.InternalApi.Model.Symbols.Declarations;
using Rubberduck.InternalApi.Services;
using Rubberduck.InternalApi.Settings;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Rubberduck.InternalApi.Execution;

public class VBExecutionContext : ServiceBase, IDiagnosticSource
{
    private readonly Stack<VBExecutionScope> _callStack = new();

    private readonly ConcurrentDictionary<Uri, ProjectSymbol> _referencedLibraries = [];

    private readonly ConcurrentDictionary<Uri, Symbol> _symbols = [];
    private readonly ConcurrentDictionary<TypedSymbol, VBTypedValue> _symbolTable = [];

    private readonly ConcurrentDictionary<Uri, LineLabelSymbol> _lineLabels = [];

    public VBExecutionContext(ILogger logger,
        RubberduckSettingsProvider settingsProvider,
        PerformanceRecordAggregator performance)
        : base(logger, settingsProvider, performance)
    {
    }

    #region environment
    public int LanguageVersion { get; init; } = 7;
    public bool Is64BitHost { get; init; }
    #endregion

    public void AddSymbol(Symbol symbol)
    {
        _symbols.TryAdd(symbol.Uri, symbol);
        if (symbol is TypedSymbol typedSymbol && typedSymbol.Type is VBType type)
        {
            _symbolTable.TryAdd(typedSymbol, type.DefaultValue);
        }
        else if (symbol is LineLabelSymbol lineLabel)
        {
            _lineLabels.TryAdd(lineLabel.ParentUri, lineLabel);
        }
    }

    public void AddSymbol(TypedSymbol symbol, VBTypedValue value)
    {
        if (symbol.Type is VBType type)
        {
            _symbols.TryAdd(symbol.Uri, symbol);
            _symbolTable.TryAdd(symbol, value);
        }
    }

    public Symbol GetSymbol(WorkspaceUri uri) => _symbols[uri];

    public void AddDiagnostic(Diagnostic diagnostic) => Diagnostics.Add(diagnostic);
    public void AddDiagnostics(VBRuntimeErrorException exception)
    {
        foreach (var diagnostic in exception.Diagnostics)
        {
            AddDiagnostic(diagnostic);
        }
    }
    public void AddDiagnostics(VBCompileErrorException exception)
    {
        foreach (var diagnostic in exception.Diagnostics)
        {
            AddDiagnostic(diagnostic);
        }
    }

    public void LoadReferencedLibrarySymbols(ProjectSymbol projectSymbol)
    {
        _referencedLibraries[projectSymbol.Uri] = projectSymbol;
        foreach (var symbol in projectSymbol.Children?.OfType<Symbol>() ?? [])
        {
            AddSymbol(symbol);
        }
    }

    public void UnloadReferencedLibrarySymbols(WorkspaceUri uri) => _referencedLibraries.TryRemove(uri, out _);

    public VBExecutionScope EnterScope(VBTypeMember member)
    {
        var scope = new VBExecutionScope(this, member);
        _callStack.Push(scope);
        return scope;
    }

    public VBExecutionScope CurrentScope => _callStack.Peek();

    public void End() => _callStack.Clear();

    public VBExecutionScope? ExitScope(VBRuntimeErrorException? error = null)
    {
        _callStack.Pop();
        if (!_callStack.Any() && error != null)
        {
            // issue a separate "unhandled error" diagnostic?
            End();
            return null;
        }

        return CurrentScope;
    }

    public VBTypeMember? GetModuleMember(Symbol symbol) =>
        _symbolTable.Keys
            .Where(e => e is ClassModuleSymbol || e is StandardModuleSymbol)
            .SelectMany(e => ((VBMemberOwnerType)e.Type!).Members)
            .SingleOrDefault(e => e.Symbol == symbol);


    /// <summary>
    /// Gets all resolved symbols in the context.
    /// </summary>
    public ImmutableHashSet<TypedSymbol> ResolvedSymbols => _symbolTable.Keys.Where(e => e.Type != null).ToImmutableHashSet();

    /// <summary>
    /// Gets all unresolved symbols in the context.
    /// </summary>
    public ImmutableHashSet<TypedSymbol> UnresolvedSymbols => _symbolTable.Keys.Where(e => e.Type is null).ToImmutableHashSet();

    /// <summary>
    /// Gets all <c>VBMemberOwnerType</c> data types in the context.
    /// </summary>
    public ImmutableHashSet<TypedSymbol> MemberOwnerTypes => _symbolTable.Keys.Where(e => e.Type is VBMemberOwnerType).ToImmutableHashSet();

    public ConcurrentBag<Diagnostic> Diagnostics { get; init; } = [];

    IEnumerable<Diagnostic> IDiagnosticSource.Diagnostics => Diagnostics.ToArray();

    /// <summary>
    /// Gets the currently held value of the specified symbol.
    /// </summary>
    public VBTypedValue GetSymbolValue(TypedSymbol symbol) => _symbolTable[symbol];
    /// <summary>
    /// Sets the currently held value of the specified symbol.
    /// </summary>
    public void SetSymbolValue(TypedSymbol symbol, VBTypedValue value) => _symbolTable[symbol] = value;

    public LineLabelSymbol? FindLineLabel(Uri parentUri, string name) => _lineLabels.TryGetValue(parentUri, out var label)
        ? label.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase) ? label : default : default;

    #region file statements
    private readonly HashSet<VBTypedValue> _openFileHandles = [];

    public void RequireFileHandle(VBTypedValue handle)
    {
        if (!_openFileHandles.Contains(handle))
        {
            AddDiagnostics(VBRuntimeErrorException.BadFileNameOrNumber(handle.Symbol!));
        }
    }

    public void OpenFile(VBTypedValue handle)
    {
        if (!_openFileHandles.Add(handle))
        {
            AddDiagnostics(VBRuntimeErrorException.FileAlreadyOpen(handle.Symbol!));
        }
    }

    public void CloseFile(VBTypedValue? handle = default)
    {
        if (handle is null)
        {
            _openFileHandles.Clear();
        }
        else
        {
            _openFileHandles.Remove(handle);
        }
    }
    #endregion
}
