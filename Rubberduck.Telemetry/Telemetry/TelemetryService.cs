﻿using Rubberduck.RPC.Proxy.SharedServices.Abstract;
using Rubberduck.RPC.Proxy.SharedServices.Server.Configuration;
using Rubberduck.RPC.Proxy.SharedServices.Telemetry.Abstract;
using Rubberduck.RPC.Telemetry;
using System.Threading.Tasks;

namespace Rubberduck.RPC.Proxy.SharedServices.Telemetry
{
    public class TelemetryService : ITelemetryService
    {
        public TelemetryService(TelemetryOptions configuration)
        {
            Configuration = configuration;
        }

        public TelemetryOptions Configuration { get; }

        /// <summary>
        /// <c>true</c> if telemetry is enabled on this server.
        /// </summary>
        public bool IsEnabled => Configuration.IsEnabled;

        TelemetryOptions IConfigurableProxy<TelemetryOptions>.Configuration { get; }

        public bool Transmit()
        {
            if (!IsEnabled)
            {
                return false;
            }

            /* TODO transmit telemetry items from db, set transmitted timestamp */
            return true;
        }

        public bool OnEvent<TEvent>(TEvent item) where TEvent : TelemetryEvent
        {
            /* TODO save telemetry item to db */
            return true;
        }

        public bool Clear()
        {
            /* TODO delete telemetry items from db */
            return true;
        }

        public Task<bool> OnEventAsync<TEvent>(TEvent item) where TEvent : TelemetryEvent
        {
            /* TODO write event to db */
            return Task.FromResult(true);
        }

        public Task<bool> TransmitAsync()
        {
            /* TODO */
            return Task.FromResult(true);
        }

        public Task<bool> OnClearAsync()
        {
            /* TODO */
            return Task.FromResult(true);
        }
    }
}
