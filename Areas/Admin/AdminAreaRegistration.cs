using System.Web.Mvc;

namespace QLNSVATC.Areas.Admin
{
    public class AdminAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get { return "Admin"; }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                name: "AD_QLPQ",
                url: "AD/ql-phan-quyen",
                defaults: new
                {
                    controller = "Role",
                    action = "Index",
                    codeBus = UrlParameter.Optional
                },
                namespaces: new[] { "QLNSVATC.Areas.Admin.Controllers" }
            );

            context.MapRoute(
                name: "AD_QLHD",
                url: "AD/ql-hoat-dong",
                defaults: new
                {
                    controller = "Activity",
                    action = "Index",
                    codeBus = UrlParameter.Optional
                },
                namespaces: new[] { "QLNSVATC.Areas.Admin.Controllers" }
            );

            context.MapRoute(
                name: "AD_QLMN",
                url: "AD/ql-menu",
                defaults: new
                {
                    controller = "Menu",
                    action = "Index",
                    codeBus = UrlParameter.Optional
                },
                namespaces: new[] { "QLNSVATC.Areas.Admin.Controllers" }
            );

            context.MapRoute(
                name: "AD_QLDT",
                url: "AD/ql-du-lieu",
                defaults: new
                {
                    controller = "Data",
                    action = "Index",
                    codeBus = UrlParameter.Optional
                },
                namespaces: new[] { "QLNSVATC.Areas.Admin.Controllers" }
            );

            context.MapRoute(
                name: "AD_Home",
                url: "AD/trang-chu",
                defaults: new
                {
                    controller = "Home",
                    action = "Index",
                    codeBus = UrlParameter.Optional
                },
                namespaces: new[] { "QLNSVATC.Areas.Admin.Controllers" }
            );

            context.MapRoute(
                name: "Admin_default",
                url: "Admin/{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "QLNSVATC.Areas.Admin.Controllers" }
            );
        }
    }
}
