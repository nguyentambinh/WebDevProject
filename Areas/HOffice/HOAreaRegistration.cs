using System.Web.Mvc;

namespace QLNSVATC.Areas.HO
{
    public class HOAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "HO";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "HO_default",
                "HO/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}