using System.Web.Mvc;

namespace QLNSVATC.Areas.HR
{
    public class HRAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get { return "HR"; }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                name: "HR_Home",
                url: "HR/trang-chu/{codeBus}",
                defaults: new
                {
                    controller = "Home",
                    action = "Index",
                    codeBus = UrlParameter.Optional
                },
                namespaces: new[] { "QLNSVATC.Areas.HR.Controllers" }
            );
            context.MapRoute(
                name: "HR_QLNV_Thongtin",
                url: "HR/ql-nhan-vien/thong-tin",
                defaults: new
                {
                    controller = "Employee",
                    action = "Information",
                    codeBus = UrlParameter.Optional
                },
                namespaces: new[] { "QLNSVATC.Areas.HR.Controllers" }
            );
            context.MapRoute(
                "HR_default",
                "HR/{controller}/{action}/{id}",
                new { controller = "Home", action = "Index", id = UrlParameter.Optional },
                namespaces: new[] { "QLNSVATC.Areas.HR.Controllers" }
            );
        }
    }
}
