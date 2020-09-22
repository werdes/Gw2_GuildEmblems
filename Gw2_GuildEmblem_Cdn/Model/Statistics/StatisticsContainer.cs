using Gw2_GuildEmblem_Cdn.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Gw2_GuildEmblem_Cdn.Model.Statistics
{
    public class StatisticsContainer
    {

        public Dictionary<string, EmblemCreationContainer> EmblemCreations { get; set; }
        public Dictionary<int, Dictionary<string, ResponseContainer>> Responses { get; set; }
        public Dictionary<int, Dictionary<string, ApiEndpointCallContainer>> ApiEndpointCalls { get; set; }
        public Dictionary<int, int> RatelimitExceedances { get; set; }
        public Dictionary<int, Dictionary<string, Dictionary<bool, int>>> Referrers { get; set; }
        public Dictionary<int, Dictionary<string, int>> CreatedEmblemsRequests { get; set; }


        public StatisticsContainer()
        {
            EmblemCreations = new Dictionary<string, EmblemCreationContainer>();
            Responses = new Dictionary<int, Dictionary<string, ResponseContainer>>();
            ApiEndpointCalls = new Dictionary<int, Dictionary<string, ApiEndpointCallContainer>>();
            RatelimitExceedances = new Dictionary<int, int>();
            Referrers = new Dictionary<int, Dictionary<string, Dictionary<bool, int>>>();
            CreatedEmblemsRequests = new Dictionary<int, Dictionary<string, int>>();
        }

    }
}