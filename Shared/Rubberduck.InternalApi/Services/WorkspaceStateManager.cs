﻿using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Execution;
using Rubberduck.InternalApi.Model.Declarations.Symbols;
using Rubberduck.InternalApi.Model.Workspace;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using Rubberduck.InternalApi.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rubberduck.InternalApi.Services;

public class WorkspaceStateManager : ServiceBase, IAppWorkspacesStateManager
{
    public event EventHandler<WorkspaceFileUriEventArgs> WorkspaceFileStateChanged = delegate { };

    private class ProjectStateManager : ServiceBase, IWorkspaceState
    {
        private readonly HashSet<Reference> _references = [];
        private readonly HashSet<Folder> _folders = [];
        private readonly DocumentContentStore _store;

        public event EventHandler<WorkspaceFileUriEventArgs> WorkspaceFileStateChanged = delegate { };

        public ProjectStateManager(ILogger logger, RubberduckSettingsProvider settingsProvider, PerformanceRecordAggregator performance,
            DocumentContentStore store)
            : base(logger, settingsProvider, performance)
        {
            _store = store;
            _store.DocumentStateChanged += WorkspaceFileStateChanged.Invoke;
            ExecutionContext = new VBExecutionContext(logger, settingsProvider, performance);
        }

        public void PublishDiagnostics(int? version, DocumentUri documentUri, IEnumerable<Diagnostic> diagnostics)
        {
            var root = WorkspaceRoot ?? throw new InvalidOperationException("Workspace root is not set");            
            var uri = documentUri.AsWorkspaceUri(root);
            if (_store.TryGetDocument(uri, out var document) && document is CodeDocumentState state /*&& document.Version >= (version ?? document.Version)*/)
            {
                _store.AddOrUpdate(uri, state.WithDiagnostics(diagnostics));
            }
        }

        public VBExecutionContext ExecutionContext { get; }

        public WorkspaceUri? WorkspaceRoot { get; set; }
        public string ProjectName { get; set; } = "Project1";

        public IEnumerable<Folder> Folders => _folders;
        public void AddFolder(Folder folder) => _folders.Add(folder);

        public void RemoveFolder(Folder folder) => _folders.Remove(folder);

        public IEnumerable<Reference> References => _references;

        public void AddHostLibraryReference(Reference reference)
        {
            reference.IsUnremovable = true;
            _references.Add(reference);
        }

        public void AddReference(Reference reference)
        {
            _references.Add(reference);
        }

        public void RemoveReference(Reference reference)
        {
            if (!reference.IsUnremovable)
            {
                _references.Remove(reference);
            }
        }

        public void SwapReferences(ref Reference first, ref Reference second)
        {
            if (first.IsUnremovable || second.IsUnremovable)
            {
                // built-in references (stdlib, hostlib) never move.
                return;
            }

            (first, second) = (second, first);
        }

        public IEnumerable<DocumentState> WorkspaceFiles
        {
            get
            {
                foreach (var file in _store.Enumerate())
                {
                    yield return file;
                }
            }
        }

        public IEnumerable<CodeDocumentState> SourceFiles
        {
            get
            {
                foreach (var file in _store.Enumerate().OfType<CodeDocumentState>())
                {
                    yield return file;
                }
            }
        }

        public bool LoadDocumentState(DocumentState file)
        {
            _store.AddOrUpdate(file.Uri, file);
            return true;
        }

        public bool LoadDocumentState(CodeDocumentState file)
        {
            _store.AddOrUpdate(file.Uri, file);
            if (file.Symbol is TypedSymbol typedSymbol)
            {
                ExecutionContext.AddToSymbolTable(typedSymbol);
            }
            return true;
        }

        public void UnloadAllFiles()
        {
            var files = _store.Enumerate().ToArray();
            foreach (var file in files)
            {
                _store.TryRemove(file.Uri);
            }
        }

        public bool TryGetWorkspaceFile(WorkspaceFileUri uri, out DocumentState? state) => _store.TryGetDocument(uri, out state);
        public bool TryGetSourceFile(WorkspaceFileUri uri, out CodeDocumentState? state)
        {
            var result = false;
            state = null;

            if (_store.TryGetDocument(uri, out var file))
            {
                if (file is CodeDocumentState sourceFile)
                {
                    state = sourceFile;
                    result = true;
                }
            }
            return result;
        }

        public bool CloseWorkspaceFile(WorkspaceFileUri uri, out DocumentState? state)
        {
            if (_store.TryGetDocument(uri, out state))
            {
                if (state!.IsOpened)
                {
                    state = state with { IsOpened = false };
                    _store.AddOrUpdate(uri, state);
                    return true;
                }
            }

            state = default;
            return false;
        }

        public bool RenameWorkspaceFile(WorkspaceFileUri oldUri, WorkspaceFileUri newUri)
        {
            if (_store.TryGetDocument(newUri, out _))
            {
                // new URI already exists... TODO check for a name collision
                return false;
            }

            if (_store.TryGetDocument(oldUri, out var oldCache))
            {
                _store.AddOrUpdate(newUri, oldCache! with { Uri = newUri });
            }

            return false;
        }

        public bool UnloadWorkspaceFile(WorkspaceFileUri uri)
        {
            if (_store.TryGetDocument(uri, out _))
            {
                return _store.TryRemove(uri);
            }

            return false;
        }

        public bool SaveWorkspaceFile(WorkspaceFileUri uri)
        {
            if (_store.TryGetDocument(uri, out var file) && file != null)
            {
                _store.AddOrUpdate(uri, file with { Version = 1 });
                return true;
            }
            return false;
        }

        public void DeleteWorkspaceUri(WorkspaceFileUri uri)
        {
            _store.TryRemove(uri);
        }

        public void DeleteWorkspaceUri(WorkspaceFolderUri uri)
        {
            var folder = _folders.SingleOrDefault(e => e.GetWorkspaceUri(uri.WorkspaceRoot) == uri);
            if (folder != null)
            {
                _folders.Remove(folder);
            }
        }
    }

    private readonly DocumentContentStore _store;

    public WorkspaceStateManager(ILogger<WorkspaceStateManager> logger, RubberduckSettingsProvider settings, PerformanceRecordAggregator performance,
        DocumentContentStore store)
        : base(logger, settings, performance)
    {
        _store = store;
        _store.DocumentStateChanged += WorkspaceFileStateChanged.Invoke;
    }

    private void OnWorkspaceDocumentStateChanged(object? sender, WorkspaceFileUriEventArgs e)
    {
        throw new NotImplementedException();
    }

    private readonly Dictionary<Uri, IWorkspaceState> _workspaces = [];
    public IWorkspaceState GetWorkspace(Uri workspaceRoot)
    {
        if (_workspaces.Count == 0)
        {
            throw new InvalidOperationException("Workspace data is empty.");
        }

        if (workspaceRoot is WorkspaceUri workspaceUri)
        {
            workspaceRoot = workspaceUri.WorkspaceRoot;
        }


        if (!_workspaces.TryGetValue(workspaceRoot, out var value))
        {
            LogWarning("Workspace URI was not found.", $"{workspaceRoot}\n{string.Join("\n*", _workspaces.Keys.Select(key => key.ToString()))}");
            throw new KeyNotFoundException("Workspace URI was not found.");
        }

        return value;
    }

    public IEnumerable<IWorkspaceState> Workspaces => _workspaces.Values;
    public IWorkspaceState? ActiveWorkspace { get; set; }

    public IWorkspaceState AddWorkspace(Uri workspaceRoot)
    {
        var state = new ProjectStateManager(Logger, SettingsProvider, Performance, _store)
        {
            WorkspaceRoot = new WorkspaceFolderUri(null, workspaceRoot)
        };
        _workspaces[workspaceRoot] = state;
        ActiveWorkspace = state;
        return state;
    }

    public void Unload(Uri workspaceRoot)
    {
        if (_workspaces.TryGetValue(workspaceRoot, out var state))
        {
            state.UnloadAllFiles();
            _workspaces.Remove(workspaceRoot);
        }
    }
}
