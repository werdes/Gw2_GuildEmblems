using System;

namespace Gw2_GuildEmblem_Cdn.Core.Model.Statistics
{
    public class ResponseContainer
    {
        public Guid GuildId { get; set; }
        public int Size { get; set; }
        public bool ServedFromCache { get; set; }
        public int Count { get; set; }
    }
}