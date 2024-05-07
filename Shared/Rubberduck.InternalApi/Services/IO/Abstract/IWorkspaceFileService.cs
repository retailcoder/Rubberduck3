using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using System;
using System.Threading.Tasks;

namespace Rubberduck.InternalApi.Services.IO.Abstract;

/// <summary>
/// A service that is responsible for workspace file I/O operations.
/// </summary>
public interface IWorkspaceFileService
{
    Task<string> ReadAsync(WorkspaceFileUri uri);
    Task WriteAsync(DocumentState document);

    Task SaveCopyAsync(DocumentState document, Uri uri);

    Task DeleteAsync(WorkspaceFileUri uri);
    Task RenameAsync(WorkspaceFileUri oldUri, WorkspaceFileUri newUri);
}