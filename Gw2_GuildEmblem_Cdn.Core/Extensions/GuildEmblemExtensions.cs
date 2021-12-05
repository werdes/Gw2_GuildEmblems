using Gw2_GuildEmblem_Cdn.Core.Model;
using Gw2Sharp.WebApi.V2.Models;
using System.Collections.Generic;
using System.Linq;

namespace Gw2_GuildEmblem_Cdn.Core.Extensions
{
    public static class GuildEmblemExtensions
    {
        /// <summary>
        /// Returns a string that contains all information necessary to create an emblem
        /// </summary>
        /// <param name="emblem"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static string ToDescriptorString(this GuildEmblem emblem, int size, List<ManipulationOption> lstOptions)
        {
            string flags = emblem.Flags.Count() > 0 
                                ? $"_{string.Join(".", emblem.Flags.Select(x => x.Value))}" 
                                : string.Empty;
            string options = lstOptions.Count > 0
                                ? $"_{string.Join(".", lstOptions)}"
                                : string.Empty;
            return $"{emblem.Background.Id}-{string.Join(".", emblem.Background.Colors)}_{emblem.Foreground.Id}-{string.Join(".", emblem.Foreground.Colors)}{flags}{options}_{size}";
        }
    }
}