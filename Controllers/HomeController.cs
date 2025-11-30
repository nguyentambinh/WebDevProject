using System;
using System.Linq;
using System.Web.Mvc;
using QLNSVATC.Models;  

namespace QLNSVATC.Controllers
{
    public class HomeController : Controller
    {
        QLNSVATCEntities db = new QLNSVATCEntities();

        public ActionResult Index(int page = 1)
        {
            int pageSize = 12; 

            var duAn = db.DUANs.ToList();
            var hopDong = db.HOPDONGs.ToList();
            var duAnHd = db.DUANTHEOHOPDONGs.ToList();
            var khachHang = db.KHACHHANGs.ToList();

            var nvQuery = db.NHANVIENs.OrderBy(x => x.MANV);

            var model = new HomeViewModel
            {
                DuAn = duAn,
                HopDong = hopDong,
                DuAnTheoHopDong = duAnHd,
                KhachHang = khachHang,

                NhanVien = nvQuery
                            .Skip((page - 1) * pageSize)
                            .Take(pageSize)
                            .ToList(),

                TotalNhanVien = nvQuery.Count(),
                CurrentPage = page,
                PageSize = pageSize
            };

            ViewBag.Positions = db.VITRICONGVIECs
                .ToDictionary(x => x.MACV, x => x.TENCV);

            return View(model);
        }
    }


}
