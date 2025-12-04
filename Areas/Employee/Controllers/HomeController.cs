using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QLNSVATC.Helper;
using QLNSVATC.Helpers;
using QLNSVATC.Models;

namespace QLNSVATC.Areas.Employee.Controllers
{
    public class HomeController : Controller
    {
        // GET: Employee/Home
        public ActionResult Index()
        {
            if (!CheckAccess.Role("EM"))
            {
                Session.Clear();
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            return View();
        }
        private QLNSVATCEntities db = new QLNSVATCEntities();

        public ActionResult Information(string id)
        {
            var userId = Session["UserId"] as string;
            var st = SettingsHelper.BuildViewBagData(db, userId);
            ViewBag.Settings = st;

            if (string.IsNullOrWhiteSpace(id))
            {
                var first = db.NHANVIENs.FirstOrDefault();
                if (first == null) return HttpNotFound();
                id = first.MANV;
            }

            var emp = (from nv in db.NHANVIENs
                        join pb in db.PHONGBANs on nv.MAPB equals pb.MAPB into gpb
                        from pb in gpb.DefaultIfEmpty()
                        join cv in db.VITRICONGVIECs on nv.MACV equals cv.MACV into gcv
                        from cv in gcv.DefaultIfEmpty()
                        join dn in db.THONGTINDOANHNGHIEPs on nv.MADN equals dn.MADN into gdn
                        from dn in gdn.DefaultIfEmpty()
                        join tt in db.THONGTINLIENHEs on nv.MANV equals tt.MANV into gtt
                        from tt in gtt.DefaultIfEmpty()
                        where nv.MANV == id
                        select new { nv, pb, cv, dn, tt })
                        .FirstOrDefault();

            if (emp == null) return HttpNotFound();

            var today = DateTime.Today;
            int? tuoi = emp.nv.NAMSINH.HasValue
                ? today.Year - emp.nv.NAMSINH.Value
                : (int?)null;

            var luong = db.LUONGs.FirstOrDefault(x => x.MANV == emp.nv.MANV);

            double? heSoLuong = db.LICHLAMVIECs
                .Where(x => x.MANV == emp.nv.MANV)
                .OrderByDescending(x => x.NGAYLAMVIEC)
                .Select(x => (double?)x.HESOLUONG)
                .FirstOrDefault();

            var fromDate = new DateTime(today.Year, today.Month, 1).AddMonths(-2);
            var toDate = today;

            double tongCa3Thang = db.CHAMCONGs
                .Where(c => c.MANV == emp.nv.MANV
                            && c.NGAYCC >= fromDate
                            && c.NGAYCC <= toDate)
                .Select(c => (double?)c.TONGCA)
                .DefaultIfEmpty(0)
                .Sum() ?? 0.0;

            var thuongPhatList = db.DSTHUONGPHATs
                .Where(x => x.MANV == emp.nv.MANV)
                .ToList();

            decimal? tongThuong = thuongPhatList
                .Where(x => x.TONG > 0)
                .Sum(x => x.TONG);

            decimal? tongPhat = thuongPhatList
                .Where(x => x.TONG < 0)
                .Sum(x => x.TONG);

            string queQuanText = emp.tt != null && emp.tt.QUEQUAN.HasValue
                ? "Mã: " + emp.tt.QUEQUAN.Value
                : null;

            var thanNhan = db.NHANTHANs
                                .Where(t => t.MANV == emp.nv.MANV)
                                .Select(t => new
                                {
                                    t.TENNT,
                                    t.QUANHE,
                                    t.DIENTHOAI,
                                    t.DIACHI
                                })
                                .FirstOrDefault();

            var model = new HREmployeeInformationViewModel
            {
                MaNV = emp.nv.MANV,
                HoLot = emp.nv.HOLOT,
                TenNV = emp.nv.TENNV,

                GioiTinh = emp.nv.GIOITINH ?? true,

                NamSinh = emp.nv.NAMSINH,
                Tuoi = tuoi,

                MaPB = emp.nv.MAPB,
                TenPhongBan = emp.pb != null ? emp.pb.TENPB : null,
                MaCV = emp.nv.MACV,
                TenChucVu = emp.cv != null ? emp.cv.TENCV : null,

                MaDN = emp.nv.MADN,
                TenDoanhNghiep = emp.dn != null ? emp.dn.TENDN : null,
                DiaChiDoanhNghiep = emp.dn != null ? emp.dn.DIACHI : null,

                NgayBatDau = emp.nv.HDLD,
                NgayHDLD = emp.nv.HDLD,

                QueQuanText = queQuanText,
                SDT = emp.tt != null ? emp.tt.SODT : null,
                Email = emp.tt != null ? emp.tt.GMAIL : null,
                DiaChi = emp.tt != null ? emp.tt.DIACHI : null,
                Facebook = emp.tt != null ? emp.tt.FB : null,

                LoaiNV = luong != null ? luong.LOAINV : null,
                HeSoLuong = heSoLuong,
                LuongCoBan = luong != null ? luong.LUONGCOBAN : null,
                NguoiThanTen = thanNhan?.TENNT,
                NguoiThanQuanHe = thanNhan?.QUANHE,
                NguoiThanSDT = thanNhan?.DIENTHOAI,
                NguoiThanDiaChi = thanNhan?.DIACHI,

                TongCa3Thang = tongCa3Thang,
                TongThuong = tongThuong,
                TongPhat = tongPhat
            };

            return View(model);
        }
    }
}