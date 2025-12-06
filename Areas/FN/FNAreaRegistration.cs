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
                name: "FN_Profit",
                url: "FN/ql-loi-nhuan",
                defaults: new { controller = "Profit", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "QLNSVATC.Areas.FN.Controllers" }
            );
            context.MapRoute(
                name: "FN_Expense/Transport",
                url: "FN/ql-chi-vcnvl",
                defaults: new { controller = "Expense", action = "Transport", id = UrlParameter.Optional },
                namespaces: new[] { "QLNSVATC.Areas.FN.Controllers" }
            );
            context.MapRoute(
                name: "FN_Expense_Project",
                url: "FN/ql-chi-du-an",
                defaults: new { controller = "Expense", action = "Project", id = UrlParameter.Optional },
                namespaces: new[] { "QLNSVATC.Areas.FN.Controllers" }
            );
            context.MapRoute(
                name: "FN_Expense_RawMaterialPurchase",
                url: "FN/ql-chi-nhapnvl",
                defaults: new { controller = "Expense", action = "RawMaterialPurchase", id = UrlParameter.Optional },
                namespaces: new[] { "QLNSVATC.Areas.FN.Controllers" }
            );
            context.MapRoute(
                name: "FN_Expense_Index",
                url: "FN/ql-chi",
                defaults: new { controller = "Expense", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "QLNSVATC.Areas.FN.Controllers" }
            );
            context.MapRoute(
                name: "FN_RevenuePJ",
                url: "FN/ql-doanh-thu-du-an",
                defaults: new { controller = "Revenue", action = "Project", id = UrlParameter.Optional },
                namespaces: new[] { "QLNSVATC.Areas.FN.Controllers" }
            );
            context.MapRoute(
                name: "FN_RevenueS",
                url: "FN/ql-doanh-thu-dich-vu",
                defaults: new { controller = "Revenue", action = "Service", id = UrlParameter.Optional },
                namespaces: new[] { "QLNSVATC.Areas.FN.Controllers" }
            );

            context.MapRoute(
                name: "FN_Home",
                url: "FN/ql-doanh-thu",
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