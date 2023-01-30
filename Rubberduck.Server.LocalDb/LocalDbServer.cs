﻿using Rubberduck.RPC.Platform;
using Rubberduck.RPC.Platform.Model;
using Rubberduck.RPC.Proxy.LocalDbServer.Abstract;
using Rubberduck.Server.LocalDb.Services;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Rubberduck.Server.LocalDb
{
    /// <summary>
    /// A <c>JsonRpcServer</c> that runs a local database and transports RPC messages over named pipe streams.
    /// </summary>
    /// <remarks>
    /// Holds the server state for the lifetime of the host process.
    /// </remarks>
    internal class LocalDbServer : JsonRpcServer<NamedPipeServerStream, LocalDbServerService, ServerCapabilities, ILocalDbServerProxyClient>
    {
        public LocalDbServer(BasicServerInfo info,
            IRpcStreamFactory<NamedPipeServerStream> rpcStreamFactory, 
            IServerServiceFactory<LocalDbServerService, ServerCapabilities, ILocalDbServerProxyClient> serviceFactory, 
            CancellationTokenSource cts) 
            : base(info, rpcStreamFactory, serviceFactory, cts.Token)
        {
        }

        protected override async Task WaitForConnectionAsync(NamedPipeServerStream stream, CancellationToken token) 
            => await stream.WaitForConnectionAsync(token);

        protected override IEnumerable<Type> GetProxyTypes() => new[]
        {
            typeof(ILocalDbServerProxyClient),
        };
    }
}
