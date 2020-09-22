using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Gw2_GuildEmblem_Cdn.Model.Statistics
{
    public class ApiEndpointCallContainer
    {
        public string Endpoint { get; set; }
        public bool ServedFromCache { get; set; }
        public int Count { get; set; }
    }
}