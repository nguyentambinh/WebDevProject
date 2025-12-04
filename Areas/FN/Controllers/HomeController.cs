using System.Linq;
using System.Web.Mvc;
using QLNSVATC.Models;
using QLNSVATC.Helpers;
using System;

namespace QLNSVATC.Areas.FN.Controllers
{
    public class HomeController : Controller
    {
        private readonly QLNSVATCEntities db = new QLNSVATCEntities();

        [HttpGet]
        public ActionResult Index()
        {
            string userId = Session["UserId"]?.ToString();
            var st = SettingsHelper.BuildViewBagData(db, userId);

            ViewBag.Settings = st;
            var vm = BuildDashboardViewModel();
            return View(vm);
        }

        public ActionResult LoadSidebar()
        {
            return PartialView("_Sidebar");
        }
        public ActionResult LoadTopCards()
        {
            return PartialView("_TopCards");
        }
        public ActionResult LoadAnalytics()
        {
            return PartialView("_Analytics");
        }
        public ActionResult LoadBank()
        {
            return PartialView("_Bank");
        }
        public ActionResult LoadTarget()
        {
            return PartialView("_Target");
        }
        public ActionResult LoadActivity()
        {
            return PartialView("_Activity");
        }

        private FNDashboardViewModel BuildDashboardViewModel()
        {
            // --- DOANH THU ---
            var revenueProjects = db.DTDUANs
                .Select(x => (decimal?)x.TIENNGHIEMTHU_TONG)
                .Sum() ?? 0m;

            var revenueServices = db.DTDICHVUs
                .Select(x => (decimal?)x.GIACATONG)
                .Sum() ?? 0m;
            var totalEarning = revenueProjects + revenueServices;


            // --- CHI PHÍ ---
            var costProjects = db.CPDUANs
                .Select(x => (decimal?)x.CHIPHITONG)
                .Sum() ?? 0m;

            var costMaterials = db.NHAPNVLs
                .Select(x => (decimal?)x.CPNHAP)
                .Sum() ?? 0m;

            var costTransport = db.VANCHUYENNVLs
                .Select(x => (decimal?)x.CPVC)
                .Sum() ?? 0m;

            var totalSpending = costProjects + costMaterials + costTransport;

            // --- ĐẾM NGHIỆP VỤ ---
            var revenueInvoiceCount = db.DTDUANs.Count() + db.DTDICHVUs.Count();

            var transactionCount = revenueInvoiceCount
                                   + db.CPDUANs.Count()
                                   + db.NHAPNVLs.Count()
                                   + db.VANCHUYENNVLs.Count();

            var today = DateTime.Today;
            var firstMonth = new DateTime(today.Year, today.Month, 1).AddMonths(-5);

            var revenueByMonth = db.DTDICHVUs
                .Where(x => x.NGAYSDDV.HasValue &&
                            x.NGAYSDDV >= firstMonth &&
                            x.NGAYSDDV <= today)
                .GroupBy(x => new { x.NGAYSDDV.Value.Year, x.NGAYSDDV.Value.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Total = g.Sum(z => (decimal?)(z.GIACATONG ?? 0)) ?? 0m
                })
                .ToList();

            var trend = new System.Collections.Generic.List<FNChartPointVM>();
            for (int i = 0; i < 6; i++)
            {
                var dt = firstMonth.AddMonths(i);
                var found = revenueByMonth.FirstOrDefault(x => x.Year == dt.Year && x.Month == dt.Month);
                trend.Add(new FNChartPointVM
                {
                    Label = dt.ToString("MM/yyyy"),
                    Value = found?.Total ?? 0m
                });
            }
            // -- NỔI BẬT --
            var revenueByProject = db.DTDUANs
                .GroupBy(x => x.MADA)
                .Select(g => new
                {
                    MADA = g.Key,
                    Revenue = g.Sum(z => (decimal?)(z.TIENNGHIEMTHU_TONG ?? 0)) ?? 0m
                })
                .ToList();

            var costByProject = db.CPDUANs
                .GroupBy(x => x.MADA)
                .Select(g => new
                {
                    MADA = g.Key,
                    Cost = g.Sum(z => (decimal?)(z.CHIPHITONG ?? 0)) ?? 0m
                })
                .ToList();

            var empByProject = db.NVTHAMGIADAs
                .Where(x => x.DUANTHEOHOPDONG != null && x.DUANTHEOHOPDONG.DUAN != null)
                .GroupBy(x => x.DUANTHEOHOPDONG.DUAN.MADA)
                .Select(g => new
                {
                    MADA = g.Key,
                    EmployeeCount = g.Select(z => z.MANV).Distinct().Count()
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

            var core = (from r in revenueByProject
                        join c in costByProject on r.MADA equals c.MADA into rc
                        from c in rc.DefaultIfEmpty()
                        select new
                        {
                            MADA = r.MADA,
                            Revenue = r.Revenue,
                            Cost = c != null ? c.Cost : 0m
                        }).ToList();

            var joined = (from rc in core
                          join e in empByProject on rc.MADA equals e.MADA into rce
                          from e in rce.DefaultIfEmpty()
                          join n in nameByProject on rc.MADA equals n.MADA into rcn
                          from n in rcn.DefaultIfEmpty()
                          select new
                          {
                              rc.MADA,
                              rc.Revenue,
                              rc.Cost,
                              EmployeeCount = e != null ? e.EmployeeCount : 0,
                              TenDuAn = n != null ? n.TenDuAn : null
                          }).ToList();

            Func<dynamic, ProjectHighlightVM> makeVm = x =>
            {
                decimal revenue = x.Revenue;
                decimal cost = x.Cost;
                decimal profit = revenue - cost;
                decimal? profitPercent = cost > 0
                    ? (decimal?)Math.Round(profit * 100m / cost, 2)
                    : null;

                return new ProjectHighlightVM
                {
                    MaDa = x.MADA,
                    ProjectName = string.IsNullOrWhiteSpace((string)x.TenDuAn)
                        ? x.MADA
                        : (string)x.TenDuAn,
                    Revenue = revenue,
                    Cost = cost,
                    Profit = profit,
                    ProfitPercent = profitPercent,
                    EmployeeCount = (int)x.EmployeeCount
                };
            };

            var topRevenue = joined
                .OrderByDescending(x => x.Revenue)
                .Take(6)
                .Select(makeVm)
                .ToList();

            var topProfit = joined
                .Where(x => x.Cost > 0)
                .OrderByDescending(x => (x.Revenue - x.Cost) / (x.Cost == 0 ? 1 : x.Cost))
                .Take(6)
                .Select(makeVm)
                .ToList();

            var topEmployee = joined
                .OrderByDescending(x => x.EmployeeCount)
                .Take(6)
                .Select(makeVm)
                .ToList();

            return new FNDashboardViewModel
            {
                TotalEarning = totalEarning,
                RevenueFromProjects = revenueProjects,
                RevenueFromServices = revenueServices,
                TotalSpending = totalSpending,
                CostForProjects = costProjects,
                CostForMaterials = costMaterials,
                CostForTransport = costTransport,
                RevenueInvoiceCount = revenueInvoiceCount,
                TransactionCount = transactionCount,
                RevenueTrend = trend,
                TopRevenueProjects = topRevenue,
                TopProfitProjects = topProfit,
                TopEmployeeProjects = topEmployee
            };
        }
    }
}
