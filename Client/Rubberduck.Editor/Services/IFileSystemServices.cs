using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Rubberduck.InternalApi.Extensions;
using System;
using System.IO.Abstractions;

namespace Rubberduck.Editor.Services;

public interface IFileSystemServices
{
    string ReadFileUri(Uri uri);
    void DeleteUri(Uri uri);
    void RenameUri(Uri oldUri, Uri newUri);
    void CopyUri(Uri srcUri, Uri dstUri);
    void CreateFolder(WorkspaceFolderUri uri);
    void CreateFile(Uri uri, string? content = null);
}

public class FileSystemServices : IFileSystemServices
{
    private readonly IFileSystem _fileSystem;

    public FileSystemServices(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public void CopyUri(Uri srcUri, Uri dstUri) => _fileSystem.File.Copy(srcUri.LocalPath, dstUri.LocalPath);

    public void CreateFile(Uri uri, string? content = null)
    {
        if (content is null)
        {
            _fileSystem.File.Create(uri.LocalPath);
        }
        else
        {
            _fileSystem.File.WriteAllText(uri.LocalPath, content);
        }
    }

    public void CreateFolder(WorkspaceFolderUri uri) => _fileSystem.Directory.CreateDirectory(uri.AbsoluteLocation.LocalPath);

    public void RenameUri(Uri oldUri, Uri newUri)
    {
        if (_fileSystem.File.Exists(oldUri.LocalPath))
        {
            _fileSystem.File.Move(oldUri.LocalPath, newUri.LocalPath);
        }
        else if( _fileSystem.Directory.Exists(oldUri.LocalPath)) 
        {
            _fileSystem.Directory.Move(oldUri.LocalPath, newUri.LocalPath);
        }
    }

    public void DeleteUri(Uri uri)
    {
        if (_fileSystem.File.Exists(uri.LocalPath))
        {
            _fileSystem.File.Delete(uri.LocalPath);
        }
        else if (_fileSystem.Directory.Exists(uri.LocalPath))
        {
            _fileSystem.Directory.Delete(uri.LocalPath);
        }
    }

    public string ReadFileUri(Uri uri) => _fileSystem.File.ReadAllText(uri.LocalPath);
}