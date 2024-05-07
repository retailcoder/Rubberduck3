using System;
using System.IO.Abstractions;
using Rubberduck.InternalApi.Services.IO.Abstract;

namespace Rubberduck.InternalApi.Services.IO;

public class PathService(IFileSystem fs) : IPathService
{
    public Uri GetWorkspaceRootUri(string workspaceLocation, string projectName) => new Uri(fs.Path.Combine(workspaceLocation, projectName));

    public string GetFileName(Uri uri) => fs.Path.GetFileName(uri.LocalPath);
    public string GetFileName(string path) => fs.Path.GetFileName(path);
    public string GetFileNameWithoutExtension(string path) => fs.Path.GetFileNameWithoutExtension(path);

    public string Combine(params string[] args) => fs.Path.Combine(args);

    public bool FolderExists(string path) => fs.Directory.Exists(path);
    public bool FileExists(string path) => fs.File.Exists(path);
}
