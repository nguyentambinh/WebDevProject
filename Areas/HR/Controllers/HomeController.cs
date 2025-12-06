using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using QLNSVATC.Helper;
using QLNSVATC.Helpers;
using QLNSVATC.Models;

namespace QLNSVATC.Areas.HR.Controllers
{
    public class HomeController : Controller
    {
        private readonly QLNSVATCEntities db = new QLNSVATCEntities();

        public ActionResult Index()
        {
            try
            {
                if (!CheckAccess.Role("HR"))
                {
                    Session.Clear();
                    return RedirectToAction("Login", "Account", new { area = "" });
                }

                var model = new HRDashboardViewModel();

                string userId = Session["UserId"] as string;
                UserViewBagModel stc = SettingsHelper.BuildViewBagData(db, userId);
                ViewBag.Settings = stc;

                model.TotalEmployees = db.NHANVIENs.Count();
                model.TotalDepartments = db.PHONGBANs.Count();

                model.TotalMale = db.NHANVIENs.Count(x => x.GIOITINH == true);
                model.TotalFemale = db.NHANVIENs.Count(x => x.GIOITINH == false);

                model.EmployeesWithInsurance = db.THONGTINBAOHIEMs
                    .Select(x => x.MANV)
                    .Distinct()
                    .Count();

                model.EmployeesWithHealthInfo = db.THONGTINSUCKHOEs
                    .Select(x => x.MANV)
                    .Distinct()
                    .Count();

                model.TopSalaries = (from nv in db.NHANVIENs
                                     join l in db.LUONGs on nv.MANV equals l.MANV
                                     join pb in db.PHONGBANs on nv.MAPB equals pb.MAPB into gpb
                                     from pb in gpb.DefaultIfEmpty()
                                     orderby l.LUONGCOBAN descending
                                     select new EmployeeRankingItem
                                     {
                                         MaNV = nv.MANV,
                                         HoTen = nv.HOLOT + " " + nv.TENNV,
                                         TenPhongBan = pb != null ? pb.TENPB : "(Chưa gán)",
                                         Value = l.LUONGCOBAN ?? 0,
                                         Unit = "VND",
                                         Label = "Lương cơ bản"
                                     })
                                     .Take(5)
                                     .ToList();

                model.TopBonuses = (from tp in db.DSTHUONGPHATs
                                    where tp.HINHTHUC == "KT"
                                    group tp by tp.MANV into g
                                    join nv in db.NHANVIENs on g.Key equals nv.MANV
                                    join pb in db.PHONGBANs on nv.MAPB equals pb.MAPB into gpb
                                    from pb in gpb.DefaultIfEmpty()
                                    orderby g.Sum(x => x.TONG) descending
                                    select new EmployeeRankingItem
                                    {
                                        MaNV = nv.MANV,
                                        HoTen = nv.HOLOT + " " + nv.TENNV,
                                        TenPhongBan = pb != null ? pb.TENPB : "(Chưa gán)",
                                        Value = g.Sum(x => x.TONG) ?? 0,
                                        Unit = "VND",
                                        Label = "Tổng thưởng"
                                    })
                                    .Take(5)
                                    .ToList();

                model.TopPenalties = (from tp in db.DSTHUONGPHATs
                                      where tp.HINHTHUC == "KL"
                                      group tp by tp.MANV into g
                                      join nv in db.NHANVIENs on g.Key equals nv.MANV
                                      join pb in db.PHONGBANs on nv.MAPB equals pb.MAPB into gpb
                                      from pb in gpb.DefaultIfEmpty()
                                      orderby g.Sum(x => x.TONG) descending
                                      select new EmployeeRankingItem
                                      {
                                          MaNV = nv.MANV,
                                          HoTen = nv.HOLOT + " " + nv.TENNV,
                                          TenPhongBan = pb != null ? pb.TENPB : "(Chưa gán)",
                                          Value = g.Sum(x => x.TONG) ?? 0,
                                          Unit = "VND",
                                          Label = "Tổng phạt"
                                      })
                                      .Take(5)
                                      .ToList();

                model.DepartmentSummaries = (from pb in db.PHONGBANs
                                             join nv in db.NHANVIENs on pb.MAPB equals nv.MAPB into nvs
                                             select new DepartmentSummaryItem
                                             {
                                                 MaPB = pb.MAPB,
                                                 TenPB = pb.TENPB,
                                                 SoNhanVien = nvs.Count(),
                                                 SoNam = nvs.Count(x => x.GIOITINH == true),
                                                 SoNu = nvs.Count(x => x.GIOITINH == false)
                                             })
                                             .OrderByDescending(x => x.SoNhanVien)
                                             .ToList();

                model.TopOvertime = (from cc in db.CHAMCONGs
                                     group cc by cc.MANV into g
                                     join nv in db.NHANVIENs on g.Key equals nv.MANV
                                     join pb in db.PHONGBANs on nv.MAPB equals pb.MAPB into gpb
                                     from pb in gpb.DefaultIfEmpty()
                                     orderby g.Sum(x => x.TONGCA) descending
                                     select new AttendanceSummaryItem
                                     {
                                         MaNV = nv.MANV,
                                         HoTen = nv.HOLOT + " " + nv.TENNV,
                                         TenPhongBan = pb != null ? pb.TENPB : "(Chưa gán)",
                                         TongCa = g.Sum(x => (double?)x.TONGCA) ?? 0
                                     })
                                     .Take(5)
                                     .ToList();

                if (model.TopSalaries != null && model.TopSalaries.Any())
                    model.MaxBaseSalary = model.TopSalaries.Max(x => x.Value);

                if (model.TopBonuses != null && model.TopBonuses.Any())
                    model.MaxBonus = model.TopBonuses.Max(x => x.Value);

                if (model.TopPenalties != null && model.TopPenalties.Any())
                    model.MaxPenalty = model.TopPenalties.Max(x => x.Value);

                if (model.TopOvertime != null && model.TopOvertime.Any())
                    model.MaxOvertime = model.TopOvertime.Max(x => x.TongCa);

                model.RecentLogs = db.ACTIVITY_LOG
                    .OrderByDescending(x => x.AcTionTime)
                    .Take(8)
                    .ToList();

                return View(model);
            }
            catch (Exception)
            {
                // Nếu có lỗi: trả model rỗng + message
                TempData["DashboardError"] = "System error while loading HR dashboard.";

                var emptyModel = new HRDashboardViewModel
                {
                    TopSalaries = new List<EmployeeRankingItem>(),
                    TopBonuses = new List<EmployeeRankingItem>(),
                    TopPenalties = new List<EmployeeRankingItem>(),
                    DepartmentSummaries = new List<DepartmentSummaryItem>(),
                    TopOvertime = new List<AttendanceSummaryItem>(),
                    RecentLogs = new List<ACTIVITY_LOG>()
                };

                return View(emptyModel);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
