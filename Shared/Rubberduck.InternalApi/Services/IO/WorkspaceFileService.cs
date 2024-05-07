using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.ServerPlatform.LanguageServer;
using Rubberduck.InternalApi.Services.IO.Abstract;
using System;
using System.IO.Abstractions;
using System.Threading.Tasks;

namespace Rubberduck.InternalApi.Services.IO;

public class WorkspaceFileService(IFileSystem fs) : IWorkspaceFileService
{
    public Task<string> ReadAsync(WorkspaceFileUri uri) => fs.File.ReadAllTextAsync(uri.AbsoluteLocation.LocalPath);

    public Task WriteAsync(DocumentState document) => fs.File.WriteAllTextAsync(document.Uri.AbsoluteLocation.LocalPath, document.Text);

    public Task SaveCopyAsync(DocumentState document, Uri uri) => fs.File.WriteAllTextAsync(uri.LocalPath, document.Text);

    public Task DeleteAsync(WorkspaceFileUri uri) => Task.Run(() => fs.File.Delete(uri.AbsoluteLocation.LocalPath));

    public Task RenameAsync(WorkspaceFileUri oldUri, WorkspaceFileUri newUri) => Task.Run(() => fs.File.Move(oldUri.AbsoluteLocation.LocalPath, newUri.AbsoluteLocation.LocalPath));
}