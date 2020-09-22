using Flatwhite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Gw2_GuildEmblem_Cdn.Custom.FlatwhiteCache
{
    public class FlatwhiteLog4netLogger : ILogger
    {
        private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public void Error(Exception ex)
        {
            _log.Error(ex);
        }

        public void Error(string message, Exception ex)
        {
            _log.Error(message, ex);
        }

        public void Info(string message)
        {
            _log.Info(message);
        }
    }
}