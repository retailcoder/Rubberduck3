using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using Rubberduck.Editor;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Workspace;
using Rubberduck.InternalApi.Services;
using Rubberduck.InternalApi.Services.IO.Abstract;
using Rubberduck.InternalApi.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;

namespace Rubberduck.UI.Services
{
    public class DocumentClientService : ServiceBase
    {
        public DocumentClientService(ILogger logger, RubberduckSettingsProvider settingsProvider, PerformanceRecordAggregator performance) 
            : base(logger, settingsProvider, performance)
        {
        }
    }

    public class WorkspaceClientService : AppWorkspacesService
    {
        private readonly LanguageClientApp _app;
        private readonly Dictionary<Uri, IFileSystemWatcher> _watchers = [];

        public WorkspaceClientService(ILogger<AppWorkspacesService> logger, RubberduckSettingsProvider settingsProvider, PerformanceRecordAggregator performance, IWorkspaceIOServices ioServices, IAppWorkspacesStateManager workspaces, LanguageClientApp app) 
            : base(logger, settingsProvider, performance, ioServices, workspaces)
        {
            _app = app;
        }

        public async override Task OnWorkspaceOpenedAsync(Uri uri)
        {
            if (_app.LanguageClient is null)
            {
                var startup = Task.Run(() => _app.StartupAsync(Settings.LanguageServerSettings.StartupSettings, uri));
                await startup.ConfigureAwait(false);

                if (startup.IsCompletedSuccessfully)
                {
                    await OnWorkspaceOpenedAsync(uri).ConfigureAwait(false);
                }
                else
                {
                    if (startup.Exception is Exception e)
                    {
                        LogException(e, "Task was faulted.");
                    }
                }
            }
            else
            {
                await OnWorkspaceOpenedAsync(uri).ConfigureAwait(false);
            }
        }

        public bool IsFileSystemWatcherEnabled(Uri root)
        {
            var localPath = root.LocalPath;
            if (localPath.EndsWith(ProjectFile.FileName))
            {
                root = new Uri(localPath[..^(ProjectFile.FileName.Length + 1)]);
            }
            return _watchers.TryGetValue(root, out var value) && value.EnableRaisingEvents;
        }

        public void EnableFileSystemWatcher(Uri root)
        {
            if (!Settings.LanguageClientSettings.WorkspaceSettings.EnableFileSystemWatchers)
            {
                LogInformation("EnableFileSystemWatchers setting is not configured to enable file system watchers. External changes are not monitored.");
                return;
            }

            if (!_watchers.TryGetValue(root, out var watcher))
            {
                watcher = ConfigureWatcher(root.LocalPath);
                _watchers[root] = watcher;
            }

            watcher.EnableRaisingEvents = true;
            LogInformation($"FileSystemWatcher is now active for workspace and subfolders.", $"WorkspaceRoot: {root}");
        }

        public void DisableFileSystemWatcher(Uri root)
        {
            if (_watchers.TryGetValue(root, out var watcher))
            {
                watcher.EnableRaisingEvents = false;
                LogInformation($"FileSystemWatcher is now deactived for workspace and subfolders.", $"WorkspaceRoot: {root}");
            }
            else
            {
                LogWarning($"There is no file system watcher configured for this workspace.", $"WorkspaceRoot: {root}");
            }
        }

        private IFileSystemWatcher ConfigureWatcher(string path)
        {
            var watcher = IOServices.CreateWatcherFor(path);
            watcher.IncludeSubdirectories = true;
            watcher.Error += OnWatcherError;
            watcher.Created += OnWatcherCreated;
            watcher.Changed += OnWatcherChanged;
            watcher.Deleted += OnWatcherDeleted;
            watcher.Renamed += OnWatcherRenamed;
            return watcher;
        }

        private void OnWatcherRenamed(object sender, RenamedEventArgs e)
        {
            // TODO prompt user about how to handle this.
            // ~> the file/folder URI must be updated regardless, but do we want to also rename any associated symbols?

            var state = Workspaces.ActiveWorkspace;
            if (state != null && state.WorkspaceRoot != null)
            {
                TryRunAction(() =>
                {
                    var oldUri = new WorkspaceFileUri(e.OldFullPath[(state.WorkspaceRoot.LocalPath + $"\\{WorkspaceUri.SourceRootName}").Length..], state.WorkspaceRoot);
                    var newUri = new WorkspaceFileUri(e.FullPath[(state.WorkspaceRoot.LocalPath + $"\\{WorkspaceUri.SourceRootName}").Length..], state.WorkspaceRoot);

                    if (state != null && state.TryGetWorkspaceFile(oldUri, out var workspaceFile) && workspaceFile is not null)
                    {
                        if (!state.RenameWorkspaceFile(oldUri, newUri, external: true))
                        {
                            // we have a problem here. name collision? validate and notify, unload conflicted file?
                            LogWarning("Rename failed.", $"OldUri: {oldUri.AbsoluteLocation}; NewUri: {newUri.AbsoluteLocation}");
                        }
                    }

                    // TODO handle renamed folder

                    var request = new DidChangeWatchedFilesParams
                    {
                        Changes = new Container<FileEvent>(
                            new FileEvent
                            {
                                Uri = oldUri,
                                Type = FileChangeType.Deleted
                            },
                            new FileEvent
                            {
                                Uri = newUri,
                                Type = FileChangeType.Created
                            })
                    };

                    // NOTE: this is different than the DidRenameFiles mechanism.
                    LogTrace("Sending DidChangeWatchedFiles LSP notification...", $"Renamed (deleted+created): {oldUri.AbsoluteLocation} -> {newUri.AbsoluteLocation}");
                    _app.LanguageClient?.Workspace.DidChangeWatchedFiles(request);
                });
            }
        }

        private void OnWatcherDeleted(object sender, FileSystemEventArgs e)
        {
            // TODO prompt user about how to handle this.
            // ~> should the folder/document be removed from the workspace/project?

            var state = Workspaces.ActiveWorkspace;
            if (state != null && state.WorkspaceRoot != null)
            {
                // files/folders are treated the same...
                var uri = new WorkspaceFileUri(e.FullPath[(state.WorkspaceRoot.LocalPath + $"\\{WorkspaceUri.SourceRootName}").Length..], state.WorkspaceRoot);
                var folderUri = new WorkspaceFolderUri(e.FullPath[(state.WorkspaceRoot.LocalPath + $"\\{WorkspaceUri.SourceRootName}").Length..], state.WorkspaceRoot);

                // ...so we try to delete both.
                state.DeleteWorkspaceUri(uri, external: true);
                state.DeleteWorkspaceUri(folderUri, external: true);

                var request = new DidChangeWatchedFilesParams
                {
                    Changes = new Container<FileEvent>(
                        new FileEvent
                        {
                            Uri = uri,
                            Type = FileChangeType.Deleted
                        })
                };

                // NOTE: this is different than the DidDeleteFiles mechanism.
                LogTrace("Sending DidChangeWatchedFiles LSP notification...", $"Deleted: {uri.AbsoluteLocation}");
                _app.LanguageClient?.Workspace.DidChangeWatchedFiles(request);
            }
        }

        private void OnWatcherChanged(object sender, FileSystemEventArgs e)
        {
            // TODO prompt user about how to handle this.
            // ~> should the document be reloaded from disk?

            var state = Workspaces.ActiveWorkspace;
            if (state != null && state.WorkspaceRoot != null)
            {
                var uri = new WorkspaceFileUri(e.FullPath[(state.WorkspaceRoot.LocalPath + $"\\{WorkspaceUri.SourceRootName}").Length..], state.WorkspaceRoot);
                var request = new DidChangeWatchedFilesParams
                {
                    Changes = new Container<FileEvent>(
                        new FileEvent
                        {
                            Uri = uri,
                            Type = FileChangeType.Changed
                        })
                };

                // NOTE: this is different than the document-level syncing mechanism.
                LogTrace("Sending DidChangeWatchedFiles LSP notification...", $"Changed: {uri}");
                _app.LanguageClient?.Workspace.DidChangeWatchedFiles(request);
            }
        }

        private void OnWatcherCreated(object sender, FileSystemEventArgs e)
        {
            // TODO prompt user about how to handle this.
            // ~> should the new file be included in the workspace/project?

            var state = Workspaces.ActiveWorkspace;
            if (state != null && state.WorkspaceRoot != null)
            {
                var relativePath = e.FullPath[(state.WorkspaceRoot.LocalPath + $"\\{WorkspaceUri.SourceRootName}").Length..];
                var uri = new Uri(relativePath);

                var request = new DidChangeWatchedFilesParams
                {
                    Changes = new Container<FileEvent>(
                        new FileEvent
                        {
                            Uri = uri,
                            Type = FileChangeType.Created
                        })
                };

                // NOTE: this is different than the document-level syncing mechanism.
                LogTrace("Sending DidChangeWatchedFiles LSP notification...", $"Created: {uri}");
                _app.LanguageClient?.Workspace.DidChangeWatchedFiles(request);
            }
        }

        private void OnWatcherError(object sender, ErrorEventArgs e)
        {
            var exception = e.GetException();
            LogException(exception);

            if (sender is IFileSystemWatcher watcher)
            {
                DisableFileSystemWatcher(new Uri(watcher.Path));
            }
        }

        //protected override void Dispose(bool disposing)
        //{
        //    foreach (var watcher in _watchers.Values)
        //    {
        //        watcher.Error -= OnWatcherError;
        //        watcher.Created -= OnWatcherCreated;
        //        watcher.Changed -= OnWatcherChanged;
        //        watcher.Deleted -= OnWatcherDeleted;
        //        watcher.Renamed -= OnWatcherRenamed;
        //        watcher.Dispose();
        //    }
        //    _watchers.Clear();
        //}
    }
}
