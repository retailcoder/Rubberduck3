﻿using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Workspace;
using Rubberduck.InternalApi.Services.IO.Abstract;
using Rubberduck.InternalApi.Settings;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;

namespace Rubberduck.UI.Shared.NewProject;

public class ProjectFileBuilder
{
    private readonly IWorkspaceIOServices _ioServices;
    private readonly RubberduckSettingsProvider _settings;

    private Uri? _uri = default;
    private string _name = "Project1";

    private readonly HashSet<File> _files = [];
    private readonly Dictionary<string, Module> _modules = [];
    private readonly HashSet<Reference> _references = [];
    private readonly HashSet<string> _folders = [];

    public ProjectFileBuilder(IWorkspaceIOServices ioServices, RubberduckSettingsProvider settings)
    {
        _settings = settings;
        _ioServices = ioServices;
    }

    private Uri DefaultUri => new(_ioServices.Path.Combine(
        _settings.Settings.LanguageClientSettings.WorkspaceSettings.DefaultWorkspaceRoot.LocalPath, _name));

    public ProjectFile Build() => new()
    {
        Rubberduck = ProjectFile.RubberduckVersion.ToString(2),
        Uri = _uri ?? DefaultUri,
        VBProject = new()
        {
            Name = _name,
            Modules = [.. _modules.Values],
            OtherFiles = [.. _files],
            References = [.. _references],
            Folders = [.. _folders],
        },
    };

    public ProjectFileBuilder WithModel(INewProjectWindowViewModel viewModel)
    {
        _uri = new Uri(_ioServices.Path.Combine(viewModel.WorkspaceLocation, viewModel.ProjectName));
        _name = viewModel.ProjectName;

        if (viewModel.SelectedProjectTemplate?.ProjectFile != null)
        {
            return WithTemplate(viewModel.SelectedProjectTemplate)
                .WithProjectName(viewModel.ProjectName);
        }
        return this;
    }

    public ProjectFileBuilder WithTemplate(ProjectTemplate template)
    {
        _name = template.ProjectFile.VBProject.Name;
        _references.UnionWith(template.ProjectFile.VBProject.References);
        _folders.UnionWith(template.ProjectFile.VBProject.Folders);
        _files.UnionWith(template.ProjectFile.VBProject.OtherFiles);

        foreach (var module in template.ProjectFile.VBProject.Modules)
        {
            _modules.TryAdd(module.Name, module);
        }

        return this;
    }

    public ProjectFileBuilder WithUri(Uri uri)
    {
        _uri = uri;
        return this;
    }

    public ProjectFileBuilder WithProjectName(string name)
    {
        _name = name;
        return this;
    }

    public ProjectFileBuilder WithModule(string uri, DocClassType? supertype = null)
    {
        var name = _ioServices.Path.GetFileNameWithoutExtension(uri);
        _modules.TryAdd(name, new()
        {
            Name = name,
            RelativeUri = uri,
            Super = supertype
        });

        return this;
    }
    public ProjectFileBuilder WithModule(Module module)
    {
        _modules.TryAdd(module.Name, module);
        return this;
    }

    public ProjectFileBuilder WithReference(Reference reference)
    {
        _references.Add(reference);
        return this;
    }

    public ProjectFileBuilder WithFile(WorkspaceFileUri uri, bool isAutoOpen = false)
    {
        var file = File.FromWorkspaceUri(uri);
        file.IsAutoOpen = isAutoOpen;

        _files.Add(file);

        var relativeFileUri = uri.MakeRelativeUri(uri.SourceRoot);
        var relativeFolderUri = string.Join('/', relativeFileUri.Segments[..^1]);

        return WithFolder(new WorkspaceFolderUri(relativeFolderUri, uri.WorkspaceRoot));
    }

    public ProjectFileBuilder WithFolder(WorkspaceFolderUri uri)
    {
        _folders.Add(Folder.FromWorkspaceUri(uri).RelativeUri);
        return this;
    }

    public ProjectFileBuilder WithReference(string uri, string name)
    {
        _references.Add(new()
        {
            Uri = uri,
            Name = name,
        });

        return this;
    }

    public ProjectFileBuilder WithReference(string uri, string name, string typeLibInfoUri)
    {
        _references.Add(new()
        {
            Uri = uri,
            Name = name,
            TypeLibInfoUri = typeLibInfoUri
        });

        return this;
    }

    public ProjectFileBuilder WithReference(string uri, Guid guid)
    {
        _references.Add(new()
        {
            Uri = uri,
            Guid = guid,
        });

        return this;
    }
}
