using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Collections.Generic;

namespace Rubberduck.InternalApi.Execution;

public interface IDiagnosticSource
{
    IEnumerable<Diagnostic> Diagnostics { get; }
}
