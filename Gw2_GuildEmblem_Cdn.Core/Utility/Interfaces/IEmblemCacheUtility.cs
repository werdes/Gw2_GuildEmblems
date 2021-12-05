using Gw2_GuildEmblem_Cdn.Core.Model;
using Gw2Sharp.WebApi.V2.Models;
using SkiaSharp;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Gw2_GuildEmblem_Cdn.Core.Utility.Interfaces
{
    public interface IEmblemCacheUtility
    {
        Task<int> GetCountEmblemsInCache();
        void SetEmblem(Guild guild, int size, List<ManipulationOption> lstOptions, SKBitmap image);
        bool TryGetEmblem(Guild guild, int size, List<ManipulationOption> lstOptions, out SKBitmap retVal);
        bool TryGetRaw(string descriptor, out SKBitmap retVal);
        string GetEmblemDescriptor(Guild guild, int size, List<ManipulationOption> lstOptions);
    }
}