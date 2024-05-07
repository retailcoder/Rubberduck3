using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Execution;
using Rubberduck.InternalApi.Model.Workspace;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using System;
using System.Collections.Generic;

namespace Rubberduck.InternalApi.Services;

public interface IWorkspaceState
{
    event EventHandler<WorkspaceFileUriEventArgs> WorkspaceFileStateChanged;
    WorkspaceUri? WorkspaceRoot { get; set; }
    string ProjectName { get; set; }
    bool IsLoaded { get; }

    void PublishDiagnostics(int? version, DocumentUri uri, IEnumerable<Diagnostic> diagnostics);

    IEnumerable<DocumentState> WorkspaceFiles { get; }
    IEnumerable<CodeDocumentState> SourceFiles { get; }

    IEnumerable<Reference> References { get; }
    void AddReference(Reference reference);
    void RemoveReference(Reference reference);

    IEnumerable<Folder> Folders { get; }
    void AddFolder(Folder folder);
    void RemoveFolder(Folder folder);

    VBExecutionContext ExecutionContext { get; }

    /// <summary>
    /// Resets the document version in the internal document store.
    /// </summary>
    /// <param name="uri">The workspace uri of the document to save.</param>
    bool ResetDocumentVersion(WorkspaceFileUri uri);
    /// <summary>
    /// Attempts to retrieve the specified file.
    /// </summary>
    /// <param name="uri">The URI referring to the file to retrieve.</param>
    /// <param name="fileInfo">The retrieved <c>WorkspaceFileInfo</c>, if found.</param>
    /// <returns><c>true</c> if the specified version was found.</returns>
    bool TryGetWorkspaceFile(WorkspaceFileUri uri, out DocumentState fileInfo);
    /// <summary>
    /// Attempts to retrieve the specified file.
    /// </summary>
    /// <param name="uri">The URI referring to the file to retrieve.</param>
    /// <param name="fileInfo">The retrieved <c>WorkspaceFileInfo</c>, if found.</param>
    /// <returns><c>true</c> if the specified version was found.</returns>
    bool TryGetSourceFile(WorkspaceFileUri uri, out CodeDocumentState fileInfo);
    /// <summary>
    /// Marks the file at the specified URI as closed in the editor.
    /// </summary>
    /// <param name="uri">The URI referring to the file to mark as closed.</param>
    /// <param name="fileInfo">Holds a non-null reference if the file was found.</param>
    /// <returns><c>true</c> if the workspace file was correctly found and marked as closed.</returns>
    bool CloseWorkspaceFile(WorkspaceFileUri uri, out DocumentState fileInfo);
    /// <summary>
    /// Loads the specified file into the workspace.
    /// </summary>
    /// <param name="file">The file (including its content) to be added.</param>
    /// <returns><c>true</c> if the file was successfully added (or overwritten) to the workspace, <c>false</c> if a newer version is already cached.</returns>
    /// <remarks>This method will overwrite a cached URI if URI matches an existing file.</remarks>
    bool LoadDocumentState(DocumentState file);
    /// <summary>
    /// Loads the specified file into the workspace.
    /// </summary>
    /// <param name="file">The file (including its content) to be added.</param>
    /// <returns><c>true</c> if the file was successfully added (or overwritten) to the workspace.</returns>
    /// <remarks>This method will overwrite a cached URI if URI matches an existing file.</remarks>
    bool LoadDocumentState(CodeDocumentState file);
    /// <summary>
    /// Renames the specified workspace URI.
    /// </summary>
    /// <param name="oldUri">The old URI.</param>
    /// <param name="newUri">The new URI.</param>
    /// <param name="external"><c>true</c> if the deletion occurred out of process, e.g. through a <c>FileSystemWatcher</c>.</param>
    /// <returns><c>true</c> if the rename was successful.</returns>
    bool RenameWorkspaceFile(WorkspaceFileUri oldUri, WorkspaceFileUri newUri, bool external = false);
    /// <summary>
    /// Adjusts the internal state for a removed workspace file.
    /// </summary>
    /// <param name="uri">A relative uri to remove from the workspace.</param>
    /// <param name="external"><c>true</c> if the deletion occurred out of process, e.g. through a <c>FileSystemWatcher</c>.</param>
    void DeleteWorkspaceUri(WorkspaceFileUri uri, bool external = false);
    /// <summary>
    /// Adjusts the internal state for a removed workspace folder.
    /// </summary>
    /// <param name="uri">A relative uri to remove from the workspace.</param>
    /// <param name="external"><c>true</c> if the deletion occurred out of process, e.g. through a <c>FileSystemWatcher</c>.</param>
    void DeleteWorkspaceUri(WorkspaceFolderUri uri, bool external = false);

    /// <summary>
    /// Unloads all held state.
    /// </summary>
    void Unload();
    /// <summary>
    /// Unloads all held state for the specified file.
    /// </summary>
    void Unload(WorkspaceFileUri uri);
}
