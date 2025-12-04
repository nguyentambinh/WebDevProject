using System.Web.Mvc;

namespace QLNSVATC.Areas.FN
{
    public class FNAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get { return "FN"; }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                name: "FN_Home",
                url: "FN/trang-chu/{codeBus}",
                defaults: new
                {
                    controller = "Home",
                    action = "Index",
                    codeBus = UrlParameter.Optional
                },
                namespaces: new[] { "QLNSVATC.Areas.FN.Controllers" }
            );
            context.MapRoute(
                "FN_default",
                "FN/{controller}/{action}/{id}",
                new { controller = "Home", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "QLNSVATC.Areas.FN.Controllers" }
            );
        }
    }
}
