namespace Gw2_GuildEmblem_Cdn.Core.Model.Statistics
{
    public class ApiEndpointCallContainer
    {
        public string Endpoint { get; set; }
        public bool ServedFromCache { get; set; }
        public int Count { get; set; }
    }
}