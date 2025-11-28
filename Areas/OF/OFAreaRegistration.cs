using System.Web.Mvc;

namespace QLNSVATC.Areas.OF
{
    public class OFAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get { return "OF"; }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                "OF_default",
                "OF/{controller}/{action}/{id}",
                new { controller = "Home", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "QLNSVATC.Areas.OF.Controllers" }
            );
        }
    }
}
