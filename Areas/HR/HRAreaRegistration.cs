using System.Web.Mvc;
using QLNSVATC.Models;

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
                url: "HR/ql-thong-tin-nhan-vien",
                defaults: new
                {
                    controller = "Employee",
                    action = "Information",
                    codeBus = UrlParameter.Optional
                },
                namespaces: new[] { "QLNSVATC.Areas.HR.Controllers" }
            ); 
            context.MapRoute(
                name: "HR_QLNV_ung-vien",
                url: "HR/xem-ho-so-ung-vien",
                defaults: new
                {
                    controller = "Employee",
                    action = "Candidate",
                    codeBus = UrlParameter.Optional
                },
                namespaces: new[] { "QLNSVATC.Areas.HR.Controllers" }
            );
            context.MapRoute(
                name: "HR_QLPB_Thongtin",
                url: "HR/ql-phong-ban",
                defaults: new
                {
                    controller = "Department",
                    action = "Information",
                    codeBus = UrlParameter.Optional
                },
                namespaces: new[] { "QLNSVATC.Areas.HR.Controllers" }
            );
            context.MapRoute(
                name: "HR_QLDA_Thongtin",
                url: "HR/ql-du-an",
                defaults: new
                {
                    controller = "Project",
                    action = "Information",
                    codeBus = UrlParameter.Optional
                },
                namespaces: new[] { "QLNSVATC.Areas.HR.Controllers" }
            );
            context.MapRoute(
                name: "HR_QLLLV",
                url: "HR/lich-lam-viec",
                defaults: new
                {
                    controller = "Work",
                    action = "Index",
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
