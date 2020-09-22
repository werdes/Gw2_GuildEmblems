using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;

namespace Gw2_GuildEmblem_Cdn.Utility
{
    public static class ThreadHelper
    {
        public static Thread Run(Action action)
        {
            Thread thread = new Thread(() => action());
            thread.Start();
            return thread;
        }
    }
}