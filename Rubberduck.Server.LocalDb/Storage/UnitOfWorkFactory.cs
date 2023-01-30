﻿using Rubberduck.Server.LocalDb.Internal;

namespace Rubberduck.Server.Storage
{
    internal class UnitOfWorkFactory : IUnitOfWorkFactory
    {
        private readonly IDbConnectionProvider _provider;

        public UnitOfWorkFactory(IDbConnectionProvider provider)
        {
            _provider = provider;
        }

        public IUnitOfWork CreateNew() => new UnitOfWork(_provider.GetOrCreateConnection());
    }
}
