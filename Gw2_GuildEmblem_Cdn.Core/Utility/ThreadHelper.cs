using System;
using System.Threading;

namespace Gw2_GuildEmblem_Cdn.Core.Utility
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