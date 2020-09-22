using Gw2Sharp.WebApi.V2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Gw2_GuildEmblem_Cdn.Extensions
{
    public static class GuildEmblemExtensions
    {
        public static string ToDescriptorString(this GuildEmblem emblem, int size)
        {
            string flags = emblem.Flags.Count() > 0 ? $"_{string.Join(".", emblem.Flags.Select(x => x.Value))}" : string.Empty;
            return $"{emblem.Background.Id}-{string.Join(".", emblem.Background.Colors)}_{emblem.Foreground.Id}-{string.Join(".", emblem.Foreground.Colors)}{flags}_{size}";
        }
    }
}