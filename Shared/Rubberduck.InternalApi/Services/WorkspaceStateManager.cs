using Microsoft.Extensions.Logging;
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

        private bool _isLoaded = true;
        public bool IsLoaded => _isLoaded;

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

        public bool CloseWorkspaceFile(WorkspaceFileUri uri, out DocumentState state)
        {
            if (_store.TryGetDocument(uri, out state))
            {
                if (state.Status == WorkspaceFileState.Opened)
                {
                    state = state.WithStatus(WorkspaceFileState.Loaded);
                    _store.AddOrUpdate(uri, state);
                    return true;
                }
            }

            state = default!;
            return false;
        }

        public bool RenameWorkspaceFile(WorkspaceFileUri oldUri, WorkspaceFileUri newUri, bool external = false)
        {
            if (external)
            {
                // an external process has renamed the file.
                // TODO prompt the user to let them know about the renamed file and
                // determine whether to rename the workspace files (or keep the renamed file around as a ghost file).
            }

            if (_store.TryGetDocument(newUri, out _))
            {
                // new URI already exists
                return false;
            }

            if (_store.TryGetDocument(oldUri, out var oldCache))
            {
                _store.AddOrUpdate(newUri, oldCache! with { Uri = newUri });
            }

            return false;
        }

        public void Unload(WorkspaceFileUri uri)
        {
            if (_store.TryGetDocument(uri, out _))
            {
                _store.TryRemove(uri);
            }
        }

        public void Unload()
        {
            _folders.Clear();
            _references.Clear();
            _store.Unload();

            _isLoaded = false;
        }

        public bool ResetDocumentVersion(WorkspaceFileUri uri)
        {
            if (_store.TryGetDocument(uri, out var file) && file != null)
            {
                _store.AddOrUpdate(uri, file with { Version = 1 });
                return true;
            }
            return false;
        }

        public void DeleteWorkspaceUri(WorkspaceFileUri uri, bool external = false)
        {
            if (external)
            {
                // since an external process deleted the file,
                // we don't know that the user wants to remove it from the workspace yet,
                // which is why we only set the document state here.
                if (_store.TryGetDocument(uri, out var state))
                {
                    _store.AddOrUpdate(uri, state.WithStatus(WorkspaceFileState.Missing));
                }

                // else: do nothing. caller may not know whether uri is for a file or a folder.
            }
            else
            {
                _store.TryRemove(uri);
            }
        }

        public void DeleteWorkspaceUri(WorkspaceFolderUri uri, bool external = false)
        {
            if (external)
            {
                // an external process deleted the folder.
                // if there's a workspace folder at this uri, we should mark it as deleted somehow
                // and mark any workspace file under it as deleted, too.
                foreach (var document in _store.Enumerate())
                {
                    if (document.Uri.RelativeUriString!.StartsWith(uri.RelativeUriString!))
                    {
                        _store.AddOrUpdate(document.Uri, document.WithStatus(WorkspaceFileState.Missing));
                    }
                }

                // just in case project is referencing a library that's under a workspace folder...
                foreach (var reference in _references)
                {
                    if (reference.Uri?.StartsWith(uri.AbsoluteLocation.LocalPath) ?? false)
                    {
                        reference.Status = WorkspaceFileState.Missing;
                    }
                }
            }
            else
            {
                // first we remove it from the separate workspace folders collection.
                var folder = _folders.SingleOrDefault(e => e.GetWorkspaceUri(uri.WorkspaceRoot) == uri);
                if (folder != null)
                {
                    
                    _folders.Remove(folder);
                }

                // next we remove any workspace file under the removed folder.
                foreach (var document in _store.Enumerate())
                {
                    if (document.Uri.RelativeUriString!.StartsWith(uri.RelativeUriString!))
                    {
                        _store.TryRemove(document.Uri);
                    }
                }
            }
        }
    }

    public event EventHandler<WorkspaceFileUriEventArgs> WorkspaceFileStateChanged = delegate { };
    private readonly DocumentContentStore _store;

    public WorkspaceStateManager(ILogger<WorkspaceStateManager> logger, RubberduckSettingsProvider settings, PerformanceRecordAggregator performance,
        DocumentContentStore store)
        : base(logger, settings, performance)
    {
        _store = store;
        _store.DocumentStateChanged += WorkspaceFileStateChanged;
    }

    private readonly Dictionary<Uri, IWorkspaceState> _workspaces = [];
    public IWorkspaceState GetWorkspace(Uri workspaceRoot)
    {
        if (_workspaces.Count == 0)
        {
            throw new InvalidOperationException("No workspace is currently loaded.");
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

    public IWorkspaceState AddWorkspace(Uri root)
    {
        var state = new ProjectStateManager(Logger, SettingsProvider, Performance, _store)
        {
            WorkspaceRoot = WorkspaceUri.ForRoot(root)
        };
        _workspaces[root] = state;
        ActiveWorkspace = state;
        return state;
    }

    public void Unload()
    {
        TryRunAction(() =>
        {
            _store.Unload();
            foreach (var uri in _workspaces.Keys)
            {
                Unload(uri);
            }
            _workspaces.Clear();
        });
    }

    public void Unload(Uri workspaceRoot)
    {
        if (_workspaces.TryGetValue(workspaceRoot, out var state))
        {
            state.Unload();
            _workspaces.Remove(workspaceRoot);

            LogInformation($"Unloaded workspace: '{workspaceRoot}'");
        }
        else
        {
            LogWarning($"Could not find a worskpace with root uri '{workspaceRoot}'");
        }
    }
}
