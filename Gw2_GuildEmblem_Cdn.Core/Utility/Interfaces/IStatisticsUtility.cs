using Gw2_GuildEmblem_Cdn.Core.Model.Statistics;
using Gw2Sharp.WebApi.V2.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Gw2_GuildEmblem_Cdn.Core.Utility.Interfaces
{
    public interface IStatisticsUtility
    {
        List<(string, StatisticsContainer)> GetFromZip(uint count);
        void RegisterApiEndpointCallAsync(string endpoint, bool servedFromCache);
        void RegisterCreationAsync(Guild guild, int size);
        void RegisterEmblemRequestAsync(string descriptor);
        void RegisterRatelimitExceedanceAsync();
        void RegisterReferrerAsync(HttpRequest request, bool servedFromCache);
        void RegisterResponseAsync(Guid requestedGuildId, int size, bool servedFromCache);
        void RegisterResponseAsync(HttpRequest request, bool cached);
    }
}