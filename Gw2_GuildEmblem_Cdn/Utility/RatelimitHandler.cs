using Gw2Sharp.WebApi.V2;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Gw2_GuildEmblem_Cdn.Utility
{
    public class RatelimitHandler
    {
        private readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private int _limit;
        private string _name;
        private Queue<DateTime> _accessTimes;

        public RatelimitHandler(int limit, string name)
        {
            _limit = limit;
            _name = name;
            _accessTimes = new Queue<DateTime>();
        }

        /// <summary>
        /// Sets a slot, dequeues old ones
        /// </summary>
        public void Set()
        {
            lock (_accessTimes)
            {
                _accessTimes.Enqueue(DateTime.Now);
                while (_accessTimes.Count > 0 &&
                       _accessTimes.Peek() < DateTime.Now.AddMinutes(-1D))
                {
                    _accessTimes.Dequeue();
                }
            }
        }

        public void Set(ApiV2BaseObject apiObj)
        {
            if (apiObj.HttpResponseInfo.CacheState != Gw2Sharp.WebApi.Http.CacheState.FromCache)
                Set();
        }

        public void Set(IEnumerable<ApiV2BaseObject> lstApiObjects)
        {
            if (lstApiObjects.Any(x => x.HttpResponseInfo.CacheState != Gw2Sharp.WebApi.Http.CacheState.FromCache))
                Set();
        }

        /// <summary>
        /// Sets a number equally distributed access-times
        /// </summary>
        /// <param name="count"></param>
        /// <param name="timeSpan"></param>
        public void Set(int count, TimeSpan timeSpan)
        {
            DateTime now = DateTime.Now;

            lock (_accessTimes)
            {
                for (int i = 0; i < count; i++)
                {
                    _accessTimes.Enqueue(now + TimeSpan.FromMilliseconds(i * (timeSpan.TotalMilliseconds / count)));
                }
            }
        }

        /// <summary>
        /// Waits for a free call slot
        /// </summary>
        public void Wait()
        {
            lock (_accessTimes)
            {
                if (_accessTimes.Count >= _limit)
                {
                    _log.Info($"[{_name}] Waiting for Ratelimit {_accessTimes.Peek()}");
                    while (_accessTimes.Peek() > DateTime.Now.AddMinutes(-1D))
                    {
                        Thread.Sleep(1000);
                    }
                }
            }
        }

        public int GetCurrentFreeCalls()
        {
            lock (_accessTimes)
            {
                return _limit - _accessTimes.Count;
            }
        }
    }
}
