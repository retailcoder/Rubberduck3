﻿using Newtonsoft.Json.Linq;
using Rubberduck.ServerPlatform.Platform.Model.Database;

namespace Rubberduck.Server.Database.RPC.Save
{
    public class SaveParameterRequest : SaveRequest<Parameter>
    {
        public SaveParameterRequest(object id, JToken @params)
            : base(id, JsonRpcMethods.SaveParameter, @params)
        {
        }
    }
}
