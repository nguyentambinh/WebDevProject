using System;
using System.Linq;
using System.Web.Mvc;
using QLNSVATC.Models;
using QLNSVATC.Helpers;
using QLNSVATC.Areas.FN.Data.FN_Models;
using System.Data.Entity;


namespace QLNSVATC.Areas.FN.Controllers
{
    public class ExpenseController : Controller
    {
        private readonly QLNSVATCEntities db = new QLNSVATCEntities();
        private void BuildSettings()
        {
            string userId = Session["UserId"] as string;
            var st = SettingsHelper.BuildViewBagData(db, userId);
            ViewBag.Settings = st;
            ViewBag.CurrentLang = st.Lang ?? "en_US";
        }

        public ActionResult Index()
        {
            BuildSettings();

            var vm = new FNExpenseViewModel();

            vm.TotalProjectCost = db.CPDUANs
                .Select(x => x.CHIPHITONG ?? 0)
                .DefaultIfEmpty(0)
                .Sum();

            vm.TotalMaterialCost = db.NHAPNVLs
                .Select(x => x.CPNHAP ?? 0)
                .DefaultIfEmpty(0)
                .Sum();

            vm.TotalTransportCost = db.VANCHUYENNVLs
                .Select(x => x.CPVC ?? 0)
                .DefaultIfEmpty(0)
                .Sum();

            vm.PendingApprovals = db.CPDUANs.Count(x => !x.CHIPHITONG.HasValue);

            vm.PendingMaterial = db.CPDUANs.Count(x => x.MANHAP != null && !x.CHIPHITONG.HasValue);
            vm.PendingTransport = db.CPDUANs.Count(x => x.MAVC != null && !x.CHIPHITONG.HasValue);

            var recent = db.CPDUANs
                .OrderByDescending(x => x.NHAPNVL.NGAYNHAP ?? x.VANCHUYENNVL.NGAYVC)
                .Take(5)
                .ToList()
                .Select(x => new RecentExpenseVM
                {
                    Type = x.MANHAP != null && x.MAVC == null ? "Material"
                         : x.MAVC != null && x.MANHAP == null ? "Transport"
                         : "Project",
                    Description = x.NHAPNVL != null
                        ? x.NHAPNVL.TENNVL
                        : x.MADA,
                    Date = x.NHAPNVL?.NGAYNHAP ?? x.VANCHUYENNVL?.NGAYVC,
                    Amount = x.CHIPHITONG ?? 0,
                    ProjectCode = x.MADA
                })
                .ToList();

            vm.RecentExpenses = recent;

            var cpByProject = db.CPDUANs
    .Where(c => c.CHIPHITONG.HasValue)
    .GroupBy(c => c.MADA)
    .Select(g => new
    {
        MADA = g.Key,
        Total = g.Sum(x => x.CHIPHITONG ?? 0)
    })
    .ToList();

            var projectTeamRaw = db.DUANTHEOHOPDONGs
                .Select(hd => new
                {
                    hd.MADA,
                    TeamName = hd.NVTHAMGIADAs
                        .Select(nv => nv.NHANVIEN.PHONGBAN.TENPB)
                        .FirstOrDefault()
                })
                .ToList();

            var projectTeam = projectTeamRaw
                .Where(x => !string.IsNullOrEmpty(x.TeamName))
                .GroupBy(x => x.MADA)
                .Select(g => new
                {
                    MADA = g.Key,
                    TeamName = g.FirstOrDefault().TeamName
                })
                .ToList();

            var teamSpending = (from cp in cpByProject
                                join pt in projectTeam
                                    on cp.MADA equals pt.MADA into j
                                from pt in j.DefaultIfEmpty()
                                group cp by (pt != null ? pt.TeamName : "Unassigned") into g
                                select new TeamSpendingVM
                                {
                                    TeamName = g.Key,
                                    Amount = g.Sum(x => x.Total)
                                })
                                .ToList();

            if (teamSpending.Any())
            {
                var max = teamSpending.Max(x => x.Amount);
                foreach (var t in teamSpending)
                {
                    t.HeightPercent = max > 0
                        ? (int)Math.Round((t.Amount / max) * 100m)
                        : 0;
                }
            }
            vm.TeamSpendings = teamSpending;



            decimal catProject = db.CPDUANs
                .Where(c => c.MANHAP == null && c.MAVC == null)
                .Select(c => c.CHIPHITONG ?? 0)
                .DefaultIfEmpty(0)
                .Sum();

            decimal catMaterial = db.CPDUANs
                .Where(c => c.MANHAP != null && c.MAVC == null)
                .Select(c => c.CHIPHITONG ?? 0)
                .DefaultIfEmpty(0)
                .Sum();

            decimal catTransport = db.CPDUANs
                .Where(c => c.MAVC != null && c.MANHAP == null)
                .Select(c => c.CHIPHITONG ?? 0)
                .DefaultIfEmpty(0)
                .Sum();

            decimal catOther = db.CPDUANs
                .Where(c => c.MAVC != null && c.MANHAP != null)
                .Select(c => c.CHIPHITONG ?? 0)
                .DefaultIfEmpty(0)
                .Sum();

            var cats = new[]
            {
    new CategorySpendingVM { CategoryCode = "Project",   Label = "Project",   Amount = catProject },
    new CategorySpendingVM { CategoryCode = "Material",  Label = "Material",  Amount = catMaterial },
    new CategorySpendingVM { CategoryCode = "Transport", Label = "Transport", Amount = catTransport },
    new CategorySpendingVM { CategoryCode = "Other",     Label = "Other",     Amount = catOther }
}.ToList();

            if (cats.Any())
            {
                var maxCat = cats.Max(x => x.Amount);
                foreach (var c in cats)
                {
                    c.HeightPercent = maxCat > 0
                        ? (int)Math.Round((c.Amount / maxCat) * 100m)
                        : 0;
                }
            }
            vm.CategorySpendings = cats;

            return View(vm);
        }


        public ActionResult RawMaterialPurchase()
        {
            BuildSettings();

            var vm = new MaterialPurchaseListVM();

            var query = db.NHAPNVLs
                .OrderByDescending(x => x.NGAYNHAP)
                .ToList();

            vm.Items = query.Select(n => new MaterialPurchaseRowVM
            {
                RequestCode = n.MANHAP,
                MaterialName = n.TENNVL,
                Amount = n.CPNHAP ?? 0,
                Date = n.NGAYNHAP,
                ProjectCode = n.CPDUANs.Select(c => c.MADA).FirstOrDefault()
            }).ToList();

            return View(vm);
        }
        public ActionResult Project(int page = 1)
        {
            BuildSettings();

            var today = DateTime.Today;

            var cpByProject = db.CPDUANs
                .GroupBy(c => c.MADA)
                .ToList()
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.CHIPHITONG ?? 0m)
                );

            var list = db.DUANTHEOHOPDONGs
                .Include(d => d.HOPDONG)
                .ToList();

            var rowsAll = list
                .Select(d =>
                {
                    decimal total = 0m;
                    if (!string.IsNullOrEmpty(d.MADA) &&
                        cpByProject.TryGetValue(d.MADA, out var t))
                    {
                        total = t;
                    }

                    var plannedEnd = d.HOPDONG?.NGAYKT_DUTINH;
                    var actualEnd = d.NGAYKT;

                    string status;
                    if (actualEnd.HasValue)
                    {
                        status = "done";
                    }
                    else if (!plannedEnd.HasValue)
                    {
                        status = "ongoing";
                    }
                    else
                    {
                        var days = (plannedEnd.Value.Date - today).TotalDays;
                        if (days < 0)
                            status = "overdue";
                        else if (days <= 7)
                            status = "near";
                        else
                            status = "ongoing";
                    }

                    return new ProjectExpenseRowVM
                    {
                        ProjectCode = d.MADA,
                        ProjectName = d.HOPDONG?.TENHD ?? d.MADA,
                        DetailLabel = d.HOPDONG?.TENHD ?? d.MADA,
                        ExpenseType = "Project",
                        ExpenseCode = d.MAHD,
                        ExpenseDate = plannedEnd,
                        Amount = total,
                        StatusCode = status
                    };
                })
                .OrderByDescending(x => x.ExpenseDate)
                .ThenBy(x => x.ProjectCode)
                .ToList();

            const int pageSize = 6;
            var totalCount = rowsAll.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            if (totalPages == 0) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var rows = rowsAll
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var vm = new ProjectExpenseListViewModel
            {
                Items = rows,
                TotalCount = totalCount,
                TotalAmount = rowsAll.Sum(x => x.Amount),
                PendingCount = rowsAll.Count(x => x.StatusCode == "ongoing" || x.StatusCode == "near"),
                ApprovedCount = rowsAll.Count(x => x.StatusCode == "done"),
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            };

            return View(vm);
        }

        [HttpGet]
        public ActionResult NewExpense()
        {
            BuildSettings();

            var vm = new NewExpenseViewModel();

            vm.ProjectOptions = db.DUANs
                .Select(d => new SelectListItem
                {
                    Value = d.MADA,
                    Text = d.MADA      
                })
                .OrderBy(x => x.Text)
                .ToList();

            vm.TypeOptions = new[]
            {
        new SelectListItem { Value = "Project",   Text = "Project" },
        new SelectListItem { Value = "Material",  Text = "Material" },
        new SelectListItem { Value = "Transport", Text = "Transport" }
    }.ToList();

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult NewExpense(NewExpenseViewModel model)
        {
            BuildSettings();

            if (string.IsNullOrWhiteSpace(model.ProjectCode))
                ModelState.AddModelError("ProjectCode", "Project is required");

            if (!model.Amount.HasValue || model.Amount.Value <= 0)
                ModelState.AddModelError("Amount", "Amount must be > 0");

            if (string.IsNullOrWhiteSpace(model.TypeCode))
                model.TypeCode = "Project";

            if (model.TypeCode == "Material" &&
                string.IsNullOrWhiteSpace(model.MaterialName))
            {
                ModelState.AddModelError("MaterialName", "Material name is required");
            }

            if (!ModelState.IsValid)
            {
                model.ProjectOptions = db.DUANs
                    .Select(d => new SelectListItem
                    {
                        Value = d.MADA,
                        Text = d.MADA
                    }).OrderBy(x => x.Text).ToList();

                model.TypeOptions = new[]
                {
            new SelectListItem { Value = "Project",   Text = "Project" },
            new SelectListItem { Value = "Material",  Text = "Material" },
            new SelectListItem { Value = "Transport", Text = "Transport" }
        }.ToList();

                return View(model);
            }

            var amount = model.Amount.Value;
            var ngay = model.Date ?? DateTime.Today;

            if (model.TypeCode == "Material")
            {
                var manhap = GenerateManhap();
                var nvl = new NHAPNVL
                {
                    MANHAP = manhap,
                    TENNVL = model.MaterialName,
                    CPNHAP = amount,
                    NGAYNHAP = ngay
                };
                db.NHAPNVLs.Add(nvl);

                var cp = new CPDUAN
                {
                    MADA = model.ProjectCode,
                    MANHAP = manhap,
                    MAVC = null,
                    CHIPHITONG = amount,
                    HSTHAYDOI = model.ChangeFactor
                };
                db.CPDUANs.Add(cp);
            }
            else if (model.TypeCode == "Transport")
            {
                var mavc = GenerateMavc();
                var vc = new VANCHUYENNVL
                {
                    MAVC = mavc,
                    CPVC = amount,
                    NGAYVC = ngay
                };
                db.VANCHUYENNVLs.Add(vc);

                var cp = new CPDUAN
                {
                    MADA = model.ProjectCode,
                    MANHAP = null,
                    MAVC = mavc,
                    CHIPHITONG = amount,
                    HSTHAYDOI = model.ChangeFactor
                };
                db.CPDUANs.Add(cp);
            }
            else
            {
                var cp = new CPDUAN
                {
                    MADA = model.ProjectCode,
                    MANHAP = null,
                    MAVC = null,
                    CHIPHITONG = amount,
                    HSTHAYDOI = model.ChangeFactor
                };
                db.CPDUANs.Add(cp);
            }

            db.SaveChanges();

            TempData["FN_Success"] = "Expense saved.";
            return RedirectToAction("Project");
        }

        private string GenerateManhap()
        {
            var last = db.NHAPNVLs
                .OrderByDescending(x => x.MANHAP)
                .Select(x => x.MANHAP)
                .FirstOrDefault();

            int num = 0;
            if (!string.IsNullOrEmpty(last))
            {
                var digits = new string(last.Reverse().TakeWhile(char.IsDigit).Reverse().ToArray());
                int.TryParse(digits, out num);
            }
            num++;
            return $"N{num:D4}";
        }

        private string GenerateMavc()
        {
            var last = db.VANCHUYENNVLs
                .OrderByDescending(x => x.MAVC)
                .Select(x => x.MAVC)
                .FirstOrDefault();

            int num = 0;
            if (!string.IsNullOrEmpty(last))
            {
                var digits = new string(last.Reverse().TakeWhile(char.IsDigit).Reverse().ToArray());
                int.TryParse(digits, out num);
            }
            num++;
            return $"VC{num:D4}";
        }
    }
}

