using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using QLNSVATC.Areas.FN.Data.FN_Models;
using QLNSVATC.Helper;
using QLNSVATC.Helpers;
using QLNSVATC.Models;

namespace QLNSVATC.Areas.FN.Controllers
{
    public class ProfitController : Controller
    {
        private readonly QLNSVATCEntities db = new QLNSVATCEntities();

        [HttpGet]
        public ActionResult Index()
        {
            if (!CheckAccess.Role("FN"))
            {
                Session.Clear();
                return RedirectToAction("Login", "Account", new { area = "" });
            }
            string userId = Session["UserId"] as string;
            var st = SettingsHelper.BuildViewBagData(db, userId);
            ViewBag.Settings = st;
            ViewBag.CurrentLang = st.Lang;

            var vm = BuildProfitOverviewViewModel();
            return View(vm);
        }

        private ProfitOverviewViewModel BuildProfitOverviewViewModel()
        {
            var projectRevenue = db.DTDUANs
                .Select(x => (decimal?)(x.TIENNGHIEMTHU_TONG ?? 0m))
                .Sum() ?? 0m;

            var serviceRevenue = db.DTDICHVUs
                .Select(x => (decimal?)(x.GIACATONG ?? 0m))
                .Sum() ?? 0m;

            decimal totalRevenue = projectRevenue + serviceRevenue;

            var projectCost = db.CPDUANs
                .Select(x => (decimal?)(x.CHIPHITONG ?? 0m))
                .Sum() ?? 0m;

            var materialCost = db.NHAPNVLs
                .Select(x => (decimal?)(x.CPNHAP ?? 0m))
                .Sum() ?? 0m;

            var transportCost = db.VANCHUYENNVLs
                .Select(x => (decimal?)(x.CPVC ?? 0m))
                .Sum() ?? 0m;

            decimal totalCost = projectCost + materialCost + transportCost;

            decimal projectProfit = projectRevenue - (projectCost + materialCost + transportCost);
            decimal serviceProfit = serviceRevenue;

            decimal totalProfit = totalRevenue - totalCost;

            decimal profitMargin = 0m;
            if (totalRevenue > 0)
                profitMargin = Math.Round(totalProfit / totalRevenue, 4);

            int projectCount = db.DUANs.Count();
            int completedProjects = db.DUANTHEOHOPDONGs
                .Count(x => x.NGAYKT.HasValue);

            int serviceOrders = db.DTDICHVUs.Count();
            var projectTrend = (
                from dahd in db.DUANTHEOHOPDONGs
                where dahd.NGAYKT.HasValue
                let year = dahd.NGAYKT.Value.Year
                let month = dahd.NGAYKT.Value.Month
                select new
                {
                    dahd.MADA,
                    Year = year,
                    Month = month
                })
                .Distinct()
                .ToList();

            var revenueByProject = db.DTDUANs
                .GroupBy(x => x.MADA)
                .Select(g => new
                {
                    MADA = g.Key,
                    Revenue = g.Sum(z => (decimal?)(z.TIENNGHIEMTHU_TONG ?? 0m)) ?? 0m
                })
                .ToList();

            var costByProject = db.CPDUANs
                .GroupBy(x => x.MADA)
                .Select(g => new
                {
                    MADA = g.Key,
                    Cost = g.Sum(z => (decimal?)(z.CHIPHITONG ?? 0m)) ?? 0m
                })
                .ToList();

            var trendCore = (
                from p in projectTrend
                join r in revenueByProject on p.MADA equals r.MADA into rg
                from r in rg.DefaultIfEmpty()
                join c in costByProject on p.MADA equals c.MADA into cg
                from c in cg.DefaultIfEmpty()
                select new
                {
                    p.Year,
                    p.Month,
                    Revenue = r != null ? r.Revenue : 0m,
                    Cost = c != null ? c.Cost : 0m
                })
                .ToList();

            var trend = trendCore
                .GroupBy(x => new { x.Year, x.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g =>
                {
                    decimal rev = g.Sum(z => z.Revenue);
                    decimal cost = g.Sum(z => z.Cost);
                    return new ProfitTrendPointVM
                    {
                        PeriodLabel = g.Key.Month.ToString("00") + "/" + g.Key.Year,
                        Revenue = rev,
                        Cost = cost,
                        Profit = rev - cost
                    };
                })
                .ToList();

            var joined = (
                from r in revenueByProject
                join c in costByProject on r.MADA equals c.MADA into rc
                from c in rc.DefaultIfEmpty()
                select new
                {
                    MADA = r.MADA,
                    Revenue = r.Revenue,
                    Cost = c != null ? c.Cost : 0m
                })
                .ToList();

            var nameByProject = db.DUANTHEOHOPDONGs
                .Where(x => x.DUAN != null && x.HOPDONG != null)
                .GroupBy(x => x.DUAN.MADA)
                .Select(g => new
                {
                    MADA = g.Key,
                    TenDuAn = g.Select(z => z.HOPDONG.TENHD).FirstOrDefault()
                })
                .ToList();

            var finalTop = (
                from rc in joined
                join n in nameByProject on rc.MADA equals n.MADA into rn
                from n in rn.DefaultIfEmpty()
                let profit = rc.Revenue - rc.Cost
                orderby profit descending
                select new ProjectHighlightVM
                {
                    MaDa = rc.MADA,
                    ProjectName = string.IsNullOrWhiteSpace(n?.TenDuAn) ? rc.MADA : n.TenDuAn,
                    Revenue = rc.Revenue,
                    Cost = rc.Cost,
                    Profit = profit,
                    ProfitPercent = rc.Cost > 0 ? (decimal?)Math.Round(profit * 100m / rc.Cost, 2) : null,
                    EmployeeCount = 0
                })
                .Take(6)
                .ToList();

            return new ProfitOverviewViewModel
            {
                TotalRevenue = totalRevenue,
                TotalCost = totalCost,
                TotalProfit = totalProfit,
                ProfitMargin = profitMargin,

                ProjectRevenue = projectRevenue,
                ServiceRevenue = serviceRevenue,
                ProjectCost = projectCost,
                MaterialCost = materialCost,
                TransportCost = transportCost,
                ProjectProfit = projectProfit,
                ServiceProfit = serviceProfit,

                ProjectCount = projectCount,
                CompletedProjectCount = completedProjects,
                ServiceOrderCount = serviceOrders,

                Trend = trend,
                TopProfitProjects = finalTop
            };
        }
    }
}
