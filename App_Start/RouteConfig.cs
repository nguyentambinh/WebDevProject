using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace QLNSVATC
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.MapRoute(
                name: "Trang chu",
                url: "trang-chu",
                defaults: new { controller = "Home", action = "Index" },
                namespaces: new[] { "QLNSVATC.Controllers" }
            );
            routes.MapRoute(
                name: "Trang chu2",
                url: "",
                defaults: new { controller = "Home", action = "Index" },
                namespaces: new[] { "QLNSVATC.Controllers" }
            );
            routes.MapRoute(
                name: "Gioi thieu",
                url: "gioi-thieu",
                defaults: new { controller = "Home", action = "About" },
                namespaces: new[] { "QLNSVATC.Controllers" }
            );
            routes.MapRoute(
                name: "Ung tuyen",
                url: "ung-tuyen",
                defaults: new { controller = "JobSeeker", action = "SubmitCV", id = UrlParameter.Optional },
                namespaces: new[] { "QLNSVATC.Controllers" }
            );
            routes.MapRoute(
                name: "Dang nhap",
                url: "dang-nhap",
                defaults: new { controller = "Account", action = "Login", id = UrlParameter.Optional },
                namespaces: new[] { "QLNSVATC.Controllers" }
            );
            routes.MapRoute(
                name: "Dang ky",
                url: "dang-ky",
                defaults: new { controller = "Account", action = "Register", id = UrlParameter.Optional },
                namespaces: new[] { "QLNSVATC.Controllers" }
            );
            routes.MapRoute(
                name: "Ho so",
                url: "ho-so/{id}",
                defaults: new
                {
                    controller = "Account",
                    action = "Profile",
                    id = UrlParameter.Optional
                },
                namespaces: new[] { "QLNSVATC.Controllers" }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "QLNSVATC.Controllers" }
            );
            
        }
    }

}

