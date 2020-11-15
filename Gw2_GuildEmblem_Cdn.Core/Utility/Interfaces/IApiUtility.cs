using Gw2Sharp.WebApi;
using Gw2Sharp.WebApi.V2.Models;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace Gw2_GuildEmblem_Cdn.Core.Utility.Interfaces
{
    public interface IApiUtility
    {
        Task<(Emblem, Emblem, List<Gw2Sharp.WebApi.V2.Models.Color>)> GetEmblemInformation(Guild guild);
        Task<Guild> GetGuild(string guildId);
        Task<Image> GetImage(RenderUrl url);
    }
}