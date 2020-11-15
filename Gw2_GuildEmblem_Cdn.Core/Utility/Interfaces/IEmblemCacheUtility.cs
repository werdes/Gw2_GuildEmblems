using Gw2Sharp.WebApi.V2.Models;
using System.Drawing;
using System.Threading.Tasks;

namespace Gw2_GuildEmblem_Cdn.Core.Utility.Interfaces
{
    public interface IEmblemCacheUtility
    {
        Task<int> GetCountEmblemsInCache();
        void SetEmblem(Guild guild, int size, Bitmap image);
        bool TryGetEmblem(Guild guild, int size, out Bitmap retVal);
        bool TryGetRaw(string descriptor, out Bitmap retVal);
    }
}