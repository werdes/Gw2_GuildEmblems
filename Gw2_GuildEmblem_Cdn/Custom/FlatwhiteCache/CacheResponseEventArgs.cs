using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Web;

namespace Gw2_GuildEmblem_Cdn.Custom.FlatwhiteCache
{
    public class CacheResponseEventArgs : EventArgs
    {
        public HttpRequestMessage Request { get; set; }
        public bool Cached { get; set; }
    }
}