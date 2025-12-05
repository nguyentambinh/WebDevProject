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
                name: "FN_Expense_Transport",
                url: "fn-quan-ly-chi/chi-van-chuyen-nvl",
                defaults: new { controller = "Expense", action = "Transport", id = UrlParameter.Optional },
                namespaces: new[] { "QLNSVATC.Areas.FN.Controllers" }
            );
            context.MapRoute(
                name: "FN_Expense_Project",
                url: "fn-quan-ly-chi/chi-du-an",
                defaults: new { controller = "Expense", action = "Project", id = UrlParameter.Optional },
                namespaces: new[] { "QLNSVATC.Areas.FN.Controllers" }
            );
            context.MapRoute(
                name: "FN_Expense_RawMaterialPurchase",
                url: "fn-quan-ly-chi/chi-nhap-nvl",
                defaults: new { controller = "Expense", action = "RawMaterialPurchase", id = UrlParameter.Optional },
                namespaces: new[] { "QLNSVATC.Areas.FN.Controllers" }
            );
            context.MapRoute(
                name: "FN_Expense_Index",
                url: "fn-quan-ly-chi",
                defaults: new { controller = "Expense", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "QLNSVATC.Areas.FN.Controllers" }
            );
            context.MapRoute(
                name: "FN_RevenuePJ",
                url: "fn-quan-ly-doanh-thu/tu-du-an",
                defaults: new { controller = "Revenue", action = "Project", id = UrlParameter.Optional },
                namespaces: new[] { "QLNSVATC.Areas.FN.Controllers" }
            );
            context.MapRoute(
                name: "FN_RevenueS",
                url: "fn-quan-ly-doanh-thu/tu-dich-vu-khac",
                defaults: new { controller = "Revenue", action = "Service", id = UrlParameter.Optional },
                namespaces: new[] { "QLNSVATC.Areas.FN.Controllers" }
            );

            context.MapRoute(
                name: "FN_Home",
                url: "fn-quan-ly-doanh-thu",
                defaults: new
                {
                    controller = "Home",
                    action = "Index",
                    codeBus = UrlParameter.Optional
                },
                namespaces: new[] { "QLNSVATC.Areas.FN.Controllers" }
            );
            context.MapRoute(
                name: "FN_default",
                url: "FN/{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "QLNSVATC.Areas.FN.Controllers" }
    );

        }
    }
}
