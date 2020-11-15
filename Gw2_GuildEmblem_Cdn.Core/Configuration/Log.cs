using Microsoft.Extensions.Logging;

namespace Gw2_GuildEmblem_Cdn.Core.Configuration
{
    public static class Log
    {
        public static ILoggerFactory Factory { get; } = new LoggerFactory();
        public static ILogger Create<T>() => Factory.CreateLogger<T>();
    }
}
