using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QLNSVATC.Helper;
using QLNSVATC.Models;

namespace QLNSVATC.Areas.HR.Controllers
{
    public class HomeController : Controller
    {
        // GET: HR/Home
        QLNSVATCEntities db = new QLNSVATCEntities();

        public ActionResult Index()
        {
 
            var duAn = db.DUANs?.ToList() ?? new List<DUAN>();
            var hopDong = db.HOPDONGs?.ToList() ?? new List<HOPDONG>();
            var nhanVien = db.NHANVIENs?.Take(18).ToList() ?? new List<NHANVIEN>();

            var vm = new HomeViewModel
            {
                DuAn = duAn,
                HopDong = hopDong,
                NhanVien = nhanVien
            };

            return View(vm);
        }

    }
}