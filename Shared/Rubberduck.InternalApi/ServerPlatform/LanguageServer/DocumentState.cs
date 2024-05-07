using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Rubberduck.InternalApi.Extensions;
using Rubberduck.InternalApi.Model.Declarations.Symbols;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Rubberduck.InternalApi.ServerPlatform.LanguageServer;

public enum WorkspaceFileState
{
    /// <summary>
    /// File belongs to an open workspace but was not loaded, or was manually unloaded.
    /// </summary>
    Unloaded,
    /// <summary>
    /// File belongs to an open workspace and is correctly loaded, but not opened in the editor.
    /// </summary>
    Loaded,
    /// <summary>
    /// File belongs to an open workspace but could not be found in the workspace folder.
    /// </summary>
    Missing,
    /// <summary>
    /// File belongs to an open workspace and exists in the workspace folder, but could not be loaded.
    /// </summary>
    LoadError,
    /// <summary>
    /// File belongs to an open workspace and is currently opened in the editor.
    /// </summary>
    Opened,
}

public record class CodeDocumentState : DocumentState
{
    public CodeDocumentState(CodeDocumentState original) 
        : base(original)
    {
        Language = original.Language;
        Foldings = original.Foldings;
        Diagnostics = original.Diagnostics;
        Symbol = original.Symbol;
        Status = original.Status;
    }

    public CodeDocumentState(WorkspaceFileUri uri, SupportedLanguage language, string text, WorkspaceFileState status = WorkspaceFileState.Unloaded, int version = 1)
        : base(uri, text, status, version)
    {
        Language = language;
    }

    public SupportedLanguage Language { get; init; }
    public IReadOnlyCollection<FoldingRange> Foldings { get; init; } = [];
    public IReadOnlyCollection<Diagnostic> Diagnostics { get; init; } = [];
    public Symbol? Symbol { get; init; }

    public CodeDocumentState WithLanguage(SupportedLanguage language) => this with { Language = language };
    public CodeDocumentState WithFoldings(IEnumerable<FoldingRange> foldings) => this with { Foldings = foldings.ToImmutableHashSet() };
    public CodeDocumentState WithDiagnostics(IEnumerable<Diagnostic> diagnostics) => this with { Diagnostics = diagnostics.ToImmutableHashSet() };
    public CodeDocumentState WithSymbol(Symbol symbol) => this with { Symbol = symbol };
}

public record class DocumentState
{
    public static int InitialVersion { get; } = 1;
    public static int InvalidVersion { get; } = -1;

    public static DocumentState MissingFile(WorkspaceFileUri uri) => new(uri, string.Empty, WorkspaceFileState.Missing, -1);
    public static DocumentState LoadError(WorkspaceFileUri uri) => new(uri, string.Empty, WorkspaceFileState.LoadError, -1);

    public DocumentState(DocumentState original)
    {
        Id = original.Id;
        Uri = original.Uri;
        Text = original.Text;
        Version = original.Version;
        Status = original.Status;
    }

    public DocumentState(WorkspaceFileUri uri, string text, WorkspaceFileState status = WorkspaceFileState.Unloaded, int version = 1)
    {
        Uri = uri;
        Text = text;
        Version = version;
        Status = status;

        Id = new TextDocumentIdentifier(uri.AbsoluteLocation);
    }

    public void Deconstruct(out WorkspaceFileUri uri, out string text)
    {
        uri = Uri;
        text = Text;
    }

    public TextDocumentIdentifier Id { get; }
    public WorkspaceFileUri Uri { get; init; }
    public string FileExtension => System.IO.Path.GetExtension(Uri.FileName);
    public string Name => Uri.FileNameWithoutExtension;

    public string Text { get; init; }
    public int Version { get; init; }

    public WorkspaceFileState Status { get; init; }

    public bool IsModified => Version != 1;

    /// <summary>
    /// Gets a copy of this record with the specified <c>Uri</c>.
    /// </summary>
    public DocumentState WithUri(WorkspaceFileUri uri) => this with { Uri = uri };
    /// <summary>
    /// Gets a copy of this record with the specified <c>Text</c> and an incremented <c>Version</c> number.
    /// </summary>
    public DocumentState WithText(string text) => this with { Text = text, Version = Version + 1 };
    /// <summary>
    /// Gets a copy of this record with the <c>Status</c> property as specified.
    /// </summary>
    public DocumentState WithStatus(WorkspaceFileState status) => this with { Status = status };


    /// <summary>
    /// When a file is saved, resetting its version to 1 removes its 'dirty' marker.
    /// </summary>
    public DocumentState WithResetVersion() => this with { Version = 1 };
}