﻿using Newtonsoft.Json.Linq;
using Rubberduck.ServerPlatform.Platform.Model.Database;

namespace Rubberduck.Server.Database.RPC.Query
{
    internal class ProjectInfoRequest : QueryRequest<ProjectInfo, ProjectInfoRequestOptions>
    {
        public ProjectInfoRequest(object id, JToken @params) 
            : base(id, JsonRpcMethods.QueryProjectInfo, @params)
        {
        }
    }
}
