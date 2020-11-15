using Gw2Sharp.WebApi.V2.Models;
using System;

namespace Gw2_GuildEmblem_Cdn.Core.Model.Statistics
{
    public class EmblemCreationContainer
    {
        public GuildEmblem Emblem { get; set; }
        public DateTime CreatedAt { get; set; }
        public int Size { get; set; }
        public Guid CreatedByGuild { get; set; }
    }
}