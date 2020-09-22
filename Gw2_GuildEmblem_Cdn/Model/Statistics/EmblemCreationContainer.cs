using Gw2Sharp.WebApi.V2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Gw2_GuildEmblem_Cdn.Model.Statistics
{
    public class EmblemCreationContainer
    {
        public GuildEmblem Emblem { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Size { get; set; }
        public Guid CreatedByGuild { get; set; }
    }
}