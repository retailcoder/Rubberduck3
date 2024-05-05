using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using System;
using System.Collections.Generic;

namespace Rubberduck.InternalApi.Services;

/// <summary>
/// Represents an object that holds the state for all workspaces known to the application.
/// </summary>
public interface IAppWorkspacesStateManager
{
    event EventHandler<WorkspaceFileUriEventArgs> WorkspaceFileStateChanged;
    /// <summary>
    /// Gets the workspace state for the specified workspace root URI.
    /// </summary>
    IWorkspaceState GetWorkspace(Uri workspaceRoot);
    /// <summary>
    /// Gets the state of all workspaces/projects.
    /// </summary>
    IEnumerable<IWorkspaceState> Workspaces { get; }
    /// <summary>
    /// Gets the state of the currently selected/active workspace/project.
    /// </summary>
    IWorkspaceState? ActiveWorkspace { get; }
    /// <summary>
    /// Creates a new workspace with the specified workspace root URI and returns an object holding its new, empty state.
    /// </summary>
    IWorkspaceState AddWorkspace(Uri workspaceRoot);
    /// <summary>
    /// Removes the state held for the specified workspace root URI.
    /// </summary>
    void Unload(Uri workspaceRoot);
    /// <summary>
    /// Removes all state held for all workspaces.
    /// </summary>
    void Unload();
}
