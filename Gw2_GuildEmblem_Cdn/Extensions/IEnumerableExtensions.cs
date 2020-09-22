using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Gw2_GuildEmblem_Cdn.Extensions
{
    public static class IEnumerableExtensions
    {
        /// <summary>
        /// Tries to return an element at said index
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="haystack"></param>
        /// <param name="idx"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool TryGet<T>(this IEnumerable<T> haystack, int idx, out T value)
        {
            if(haystack.Count() > idx && idx >= 0)
            {
                value = haystack.ElementAt(idx);
                return true;
            }
            value = default(T);
            return false;
        }

        public static string Join<T>(this IEnumerable<T> lst, string separator)
        {
            string[] strings = lst.Select(x => x.ToString()).ToArray();
            return string.Join(separator, strings);
        }
    }
}