using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using QLNSVATC.Models;
using QLNSVATC.Helpers;
using System.Data.Entity;
using System.Text;
using QLNSVATC.Models.FN_Models;

namespace QLNSVATC.Areas.FN.Controllers
{
    public class RevenueController : Controller
    {
        private readonly QLNSVATCEntities db = new QLNSVATCEntities();

        public ActionResult Project(DateTime? from, DateTime? to)
        {
            string userId = Session["UserId"] as string;
            var st = SettingsHelper.BuildViewBagData(db, userId);
            ViewBag.Settings = st;
            ViewBag.CurrentLang = st.Lang;

            DateTime today = DateTime.Today;
            DateTime toDate = to ?? today;
            DateTime fromDate = from ?? new DateTime(toDate.AddMonths(-11).Year,
                                                     toDate.AddMonths(-11).Month,
                                                     1);

            var projQuery = from dt in db.DTDUANs
                            join dahd in db.DUANTHEOHOPDONGs
                                on dt.MADA equals dahd.MADA
                            where dt.TIENNGHIEMTHU_TONG != null
                                  && dahd.NGAYKT != null
                                  && dahd.NGAYKT >= fromDate
                                  && dahd.NGAYKT <= toDate
                            select new { dt, dahd };

            decimal totalRevenue = projQuery.Sum(x => (decimal?)x.dt.TIENNGHIEMTHU_TONG) ?? 0m;
            int completedProjects = projQuery.Select(x => x.dt.MADA).Distinct().Count();
            decimal avgPerProject = completedProjects > 0
                ? totalRevenue / completedProjects
                : 0m;

            var projByMonth = projQuery
                .GroupBy(x => new { x.dahd.NGAYKT.Value.Year, x.dahd.NGAYKT.Value.Month })
                .Select(g => new RevenueMonthVM
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(x => (decimal)x.dt.TIENNGHIEMTHU_TONG)
                })
                .ToList();

            var market = db.BIENDONGTHITRUONGs
                .Where(x => x.THOIGIAN >= fromDate && x.THOIGIAN <= toDate)
                .GroupBy(x => new { x.THOIGIAN.Year, x.THOIGIAN.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Index = g.Average(x => x.HSTHITRUONG)
                }).ToList();

            var map = projByMonth.ToDictionary(
                x => (x.Year, x.Month),
                x => x);

            foreach (var mk in market)
            {
                var key = (mk.Year, mk.Month);
                if (map.TryGetValue(key, out var monthVm))
                {
                    monthVm.MarketIndex = mk.Index;
                }
                else
                {
                    map[key] = new RevenueMonthVM
                    {
                        Year = mk.Year,
                        Month = mk.Month,
                        MarketIndex = mk.Index,
                        Revenue = 0
                    };
                }
            }

            var revenueByMonth = map.Values
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToList();

            var revenueByRegion = db.DTDUANs
                .Where(x => x.TIENNGHIEMTHU_TONG != null && x.DTTHEOKV != null)
                .GroupBy(x => new { x.MAKV, x.DTTHEOKV.TENTINH })
                .Select(g => new ProjectRegionVM
                {
                    RegionCode = g.Key.MAKV,
                    RegionName = g.Key.TENTINH,
                    Revenue = g.Sum(x => (decimal)x.TIENNGHIEMTHU_TONG)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(6)
                .ToList();

            var topProjects = db.DTDUANs
                .Where(x => x.TIENNGHIEMTHU_TONG != null)
                .OrderByDescending(x => x.TIENNGHIEMTHU_TONG)
                .Take(10)
                .Select(x => new TopProjectVM
                {
                    ProjectCode = x.MADA,
                    ProjectName = x.DUAN.DUANTHEOHOPDONGs
                                      .Select(d => d.HOPDONG.TENHD)
                                      .FirstOrDefault() ?? x.MADA,

                    Revenue = (decimal)x.TIENNGHIEMTHU_TONG,
                    RegionName = x.DTTHEOKV != null ? x.DTTHEOKV.TENTINH : null,
                    TypeName = x.DTTHEOLHCT != null ? x.DTTHEOLHCT.TENLH : null
                })
                .ToList();


            var vm = new ProjectRevenueViewModel
            {
                TotalRevenue = totalRevenue,
                CompletedProjects = completedProjects,
                AveragePerProject = avgPerProject,
                RevenueByMonth = revenueByMonth,
                RevenueByRegion = revenueByRegion,
                TopProjects = topProjects,
                FromDate = fromDate,
                ToDate = toDate
            };

            return View(vm);
        }

        public ActionResult Service(DateTime? from, DateTime? to)
        {
            string userId = Session["UserId"] as string;
            var st = SettingsHelper.BuildViewBagData(db, userId);
            ViewBag.Settings = st;
            ViewBag.CurrentLang = st.Lang;

            DateTime today = DateTime.Today;
            DateTime toDate = to ?? today;
            DateTime fromDate = from ?? new DateTime(toDate.AddMonths(-11).Year,
                                                     toDate.AddMonths(-11).Month,
                                                     1);

            var svcQuery = db.DTDICHVUs
                .Where(x => x.NGAYSDDV != null
                            && x.NGAYSDDV >= fromDate
                            && x.NGAYSDDV <= toDate
                            && x.GIACATONG != null);

            decimal totalRevenue = svcQuery.Sum(x => (decimal?)x.GIACATONG) ?? 0m;
            int orders = svcQuery.Count();
            decimal avgOrder = orders > 0 ? totalRevenue / orders : 0m;

            var revenueByMonth = svcQuery
                .GroupBy(x => new { x.NGAYSDDV.Value.Year, x.NGAYSDDV.Value.Month })
                .Select(g => new RevenueMonthVM
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(x => (decimal)x.GIACATONG)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToList();

            var topServices = svcQuery
                .GroupBy(x => x.LHDICHVU)
                .Select(g => new TopServiceVM
                {
                    ServiceType = g.Key,
                    Revenue = g.Sum(x => (decimal)x.GIACATONG),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Revenue)
                .Take(10)
                .ToList();

            var vm = new ServiceRevenueViewModel
            {
                TotalRevenue = totalRevenue,
                ServiceOrders = orders,
                AverageOrderValue = avgOrder,
                RevenueByMonth = revenueByMonth,
                TopServices = topServices,
                FromDate = fromDate,
                ToDate = toDate
            };
            return View(vm);
        }
    }
}
