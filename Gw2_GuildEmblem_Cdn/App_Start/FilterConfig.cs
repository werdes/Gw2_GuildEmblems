using System.Web;
using System.Web.Mvc;

namespace Gw2_GuildEmblem_Cdn
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
