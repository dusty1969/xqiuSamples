using System.Web;
using System.Web.Mvc;

namespace MVCSPA_CS_VS2012._2RC
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}