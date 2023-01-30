﻿using Rubberduck.RPC.Platform.Metadata;
using Rubberduck.RPC.Proxy.LspServer.Server.Commands.Parameters;
using System.Text.Json.Serialization;

namespace Rubberduck.RPC.Proxy.LspServer.TextDocument.Language.Commands.Parameters
{
    public class CallHierarchyPrepareParams : TextDocumentPositionParams, IWorkDoneProgressParams
    {
        /// <summary>
        /// A token that the server can use to report work done progress.
        /// </summary>
        [JsonPropertyName("workDoneToken"), LspCompliant]
        public string WorkDoneToken { get; set; }
    }
}
