using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using ClosedXML.Excel;
using QLNSVATC.Areas.FN.Data.FN_Models;
using QLNSVATC.Helpers;
using QLNSVATC.Models;

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
            try
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
                    .Include(x => x.NHAPNVL)
                    .Include(x => x.VANCHUYENNVL)
                    .OrderByDescending(x => x.NHAPNVL.NGAYNHAP ?? x.VANCHUYENNVL.NGAYVC)
                    .Take(5)
                    .ToList()
                    .Select(x => new RecentExpenseVM
                    {
                        Type = x.NHAPNVL != null && (x.NHAPNVL.CPNHAP ?? 0) > 0 && (x.VANCHUYENNVL == null || (x.VANCHUYENNVL.CPVC ?? 0) == 0)
                            ? "Material"
                            : x.VANCHUYENNVL != null && (x.VANCHUYENNVL.CPVC ?? 0) > 0 && (x.NHAPNVL == null || (x.NHAPNVL.CPNHAP ?? 0) == 0)
                                ? "Transport"
                                : ((x.NHAPNVL == null || (x.NHAPNVL.CPNHAP ?? 0) == 0) &&
                                   (x.VANCHUYENNVL == null || (x.VANCHUYENNVL.CPVC ?? 0) == 0))
                                    ? "Project"
                                    : "Other",
                        Description = x.NHAPNVL != null && !string.IsNullOrEmpty(x.NHAPNVL.TENNVL)
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

                var catsQuery = db.CPDUANs
                    .Include(c => c.NHAPNVL)
                    .Include(c => c.VANCHUYENNVL)
                    .ToList();

                decimal catProject = catsQuery
                    .Where(c =>
                        ((c.NHAPNVL == null) || (c.NHAPNVL.CPNHAP ?? 0) == 0) &&
                        ((c.VANCHUYENNVL == null) || (c.VANCHUYENNVL.CPVC ?? 0) == 0))
                    .Select(c => c.CHIPHITONG ?? 0)
                    .DefaultIfEmpty(0)
                    .Sum();

                decimal catMaterial = catsQuery
                    .Where(c =>
                        (c.NHAPNVL != null && (c.NHAPNVL.CPNHAP ?? 0) > 0) &&
                        ((c.VANCHUYENNVL == null) || (c.VANCHUYENNVL.CPVC ?? 0) == 0))
                    .Select(c => c.CHIPHITONG ?? 0)
                    .DefaultIfEmpty(0)
                    .Sum();

                decimal catTransport = catsQuery
                    .Where(c =>
                        (c.VANCHUYENNVL != null && (c.VANCHUYENNVL.CPVC ?? 0) > 0) &&
                        ((c.NHAPNVL == null) || (c.NHAPNVL.CPNHAP ?? 0) == 0))
                    .Select(c => c.CHIPHITONG ?? 0)
                    .DefaultIfEmpty(0)
                    .Sum();

                decimal catOther = catsQuery
                    .Where(c =>
                        (c.NHAPNVL != null && (c.NHAPNVL.CPNHAP ?? 0) > 0) &&
                        (c.VANCHUYENNVL != null && (c.VANCHUYENNVL.CPVC ?? 0) > 0))
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
            catch (Exception)
            {
                TempData["FN_Error"] = "Unable to load expense overview page.";
                return View(new FNExpenseViewModel());
            }
        }

        public ActionResult RawMaterialPurchase()
        {
            try
            {
                BuildSettings();

                var vm = new MaterialPurchaseListVM();
                var today = DateTime.Today;

                var projectMeta = db.DUANTHEOHOPDONGs
                    .Include(d => d.HOPDONG)
                    .ToList()
                    .Select(d =>
                    {
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

                        return new
                        {
                            d.MADA,
                            ProjectName = d.HOPDONG?.TENHD ?? d.MADA,
                            StatusCode = status
                        };
                    })
                    .GroupBy(x => x.MADA)
                    .ToDictionary(g => g.Key, g => g.First());

                var cpQuery = db.CPDUANs
                    .Include(c => c.NHAPNVL)
                    .Where(c => c.NHAPNVL != null && (c.NHAPNVL.CPNHAP ?? 0) > 0)
                    .OrderByDescending(c => c.NHAPNVL.NGAYNHAP)
                    .ToList();

                vm.Items = cpQuery.Select(c =>
                {
                    projectMeta.TryGetValue(c.MADA, out var pi);

                    return new MaterialPurchaseRowVM
                    {
                        RequestCode = c.MANHAP,
                        MaterialName = c.NHAPNVL?.TENNVL,
                        Amount = c.NHAPNVL?.CPNHAP ?? 0m,
                        Date = c.NHAPNVL?.NGAYNHAP,
                        ProjectCode = c.MADA,
                        ProjectName = pi?.ProjectName ?? c.MADA,
                        ProjectStatusCode = pi?.StatusCode ?? "unknown",
                        ChangeFactor = c.HSTHAYDOI
                    };
                }).ToList();

                return View(vm);
            }
            catch (Exception)
            {
                TempData["FN_Error"] = "Unable to load raw material purchases list.";
                return View(new MaterialPurchaseListVM());
            }
        }

        public ActionResult Project(int page = 1)
        {
            try
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
            catch (Exception)
            {
                TempData["FN_Error"] = "Unable to load project expenses list.";
                return View(new ProjectExpenseListViewModel
                {
                    Items = new List<ProjectExpenseRowVM>(),
                    TotalCount = 0,
                    TotalAmount = 0,
                    PendingCount = 0,
                    ApprovedCount = 0,
                    Page = 1,
                    PageSize = 6,
                    TotalPages = 1
                });
            }
        }

        [HttpGet]
        public ActionResult GetMaterialDetail(string requestCode, string projectCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(requestCode))
                {
                    return Json(new { success = false, message = "Missing request code." },
                        JsonRequestBehavior.AllowGet);
                }

                var today = DateTime.Today;

                var cp = db.CPDUANs
                    .Include(c => c.NHAPNVL)
                    .Include(c => c.DUAN.DUANTHEOHOPDONGs.Select(d => d.HOPDONG))
                    .FirstOrDefault(c =>
                        c.MANHAP == requestCode &&
                        (string.IsNullOrEmpty(projectCode) || c.MADA == projectCode));

                if (cp == null)
                {
                    return Json(new { success = false, message = "Material record not found." },
                        JsonRequestBehavior.AllowGet);
                }

                var duan = cp.DUAN;
                var dahd = duan?.DUANTHEOHOPDONGs.FirstOrDefault();
                var hopdong = dahd?.HOPDONG;

                string status = "unknown";
                if (dahd != null)
                {
                    var plannedEnd = hopdong?.NGAYKT_DUTINH;
                    var actualEnd = dahd.NGAYKT;

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
                }

                var nvl = cp.NHAPNVL;

                var detail = new
                {
                    success = true,
                    code = cp.MANHAP,
                    materialName = nvl?.TENNVL ?? "",
                    amount = nvl?.CPNHAP ?? 0m,
                    amountText = (nvl?.CPNHAP ?? 0m).ToString("#,##0"),
                    date = nvl?.NGAYNHAP?.ToString("dd/MM/yyyy") ?? "",
                    projectCode = cp.MADA,
                    projectName = hopdong?.TENHD ?? cp.MADA,
                    projectStatus = status,
                    changeFactor = cp.HSTHAYDOI,
                    changeFactorText = cp.HSTHAYDOI.HasValue ? cp.HSTHAYDOI.Value.ToString("0.##") : "",
                    totalCost = cp.CHIPHITONG ?? 0m,
                    totalCostText = (cp.CHIPHITONG ?? 0m).ToString("#,##0"),
                    deposit = duan?.TIENCOC ?? 0m,
                    depositText = (duan?.TIENCOC ?? 0m).ToString("#,##0"),
                    expectedRevenue = duan?.TIENNGHIEMTHU_DUTINH ?? 0m,
                    expectedRevenueText = (duan?.TIENNGHIEMTHU_DUTINH ?? 0m).ToString("#,##0")
                };

                return Json(detail, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error while loading material detail." },
                    JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult DeleteMaterial(string requestCode, string projectCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(requestCode))
                {
                    return Json(new { success = false, message = "Missing request code." });
                }

                var nvl = db.NHAPNVLs
                    .Include(n => n.CPDUANs)
                    .FirstOrDefault(n => n.MANHAP == requestCode);

                if (nvl == null)
                {
                    return Json(new { success = false, message = "Material record not found." });
                }

                foreach (var cp in nvl.CPDUANs.ToList())
                {
                    db.CPDUANs.Remove(cp);
                }

                db.NHAPNVLs.Remove(nvl);
                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Material record and linked project costs have been deleted."
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error while deleting material record." });
            }
        }

        [HttpGet]
        public ActionResult NewExpense()
        {
            try
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

                vm.TypeCode = "Project";

                return View(vm);
            }
            catch (Exception)
            {
                TempData["FN_Error"] = "Unable to open new expense form.";
                return RedirectToAction("Project");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult NewExpense(NewExpenseViewModel model)
        {
            try
            {
                BuildSettings();

                // ==== VALIDATION ====
                if (string.IsNullOrWhiteSpace(model.ProjectCode))
                    ModelState.AddModelError("ProjectCode", "Project is required.");

                if (!model.Amount.HasValue || model.Amount.Value <= 0)
                    ModelState.AddModelError("Amount", "Amount must be greater than zero.");

                if (string.IsNullOrWhiteSpace(model.TypeCode))
                    model.TypeCode = "Project";

                if (model.TypeCode == "Material" &&
                    string.IsNullOrWhiteSpace(model.MaterialName))
                {
                    ModelState.AddModelError("MaterialName", "Material name is required.");
                }

                if (!ModelState.IsValid)
                {
                    model.ProjectOptions = db.DUANs
                        .Select(d => new SelectListItem
                        {
                            Value = d.MADA,
                            Text = d.MADA
                        })
                        .OrderBy(x => x.Text)
                        .ToList();

                    model.TypeOptions = new[]
                    {
                new SelectListItem { Value = "Project",   Text = "Project" },
                new SelectListItem { Value = "Material",  Text = "Material" },
                new SelectListItem { Value = "Transport", Text = "Transport" }
            }.ToList();

                    return View(model);
                }

                decimal amount = model.Amount.Value;
                decimal factor = model.ChangeFactor ?? 1m;
                decimal totalCost = amount * factor;
                DateTime ngay = model.Date ?? DateTime.Today;

                using (var tran = db.Database.BeginTransaction())
                {
                    try
                    {
                        string manhap = GenerateManhap();
                        string mavc = GenerateMavc();

                        string materialName;
                        decimal cpNhap;
                        decimal cpVc;

                        switch (model.TypeCode)
                        {
                            case "Material":
                                materialName = model.MaterialName;
                                cpNhap = amount;
                                cpVc = 0m;
                                break;

                            case "Transport":
                                materialName = "[AUTO] Transport-only";
                                cpNhap = 0m;
                                cpVc = amount;
                                break;

                            default:
                                materialName = "[AUTO] Project expense";
                                cpNhap = 0m;
                                cpVc = 0m;
                                break;
                        }

                        var nvl = new NHAPNVL
                        {
                            MANHAP = manhap,
                            TENNVL = materialName,
                            CPNHAP = cpNhap,
                            NGAYNHAP = ngay
                        };
                        db.NHAPNVLs.Add(nvl);

                        var vc = new VANCHUYENNVL
                        {
                            MAVC = mavc,
                            CPVC = cpVc,
                            NGAYVC = ngay
                        };
                        db.VANCHUYENNVLs.Add(vc);

                        var cp = new CPDUAN
                        {
                            MADA = model.ProjectCode,
                            MANHAP = manhap,
                            MAVC = mavc,
                            CHIPHITONG = totalCost,
                            HSTHAYDOI = model.ChangeFactor
                        };
                        db.CPDUANs.Add(cp);

                        db.SaveChanges();
                        tran.Commit();
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }

                TempData["FN_Success"] = "Expense has been saved successfully.";
                return RedirectToAction("Project");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while saving the expense.");
                if (ex.InnerException != null)
                    ModelState.AddModelError("", "Inner: " + ex.InnerException.Message);
                if (ex.InnerException != null && ex.InnerException.InnerException != null)
                    ModelState.AddModelError("", "Inner 2: " + ex.InnerException.InnerException.Message);

                // load lại dropdown
                model.ProjectOptions = db.DUANs
                    .Select(d => new SelectListItem
                    {
                        Value = d.MADA,
                        Text = d.MADA
                    })
                    .OrderBy(x => x.Text)
                    .ToList();

                model.TypeOptions = new[]
                {
            new SelectListItem { Value = "Project",   Text = "Project" },
            new SelectListItem { Value = "Material",  Text = "Material" },
            new SelectListItem { Value = "Transport", Text = "Transport" }
        }.ToList();

                return View(model);
            }
        }

        private string GenerateManhap()
        {
            const string prefix = "N";

            var codes = db.NHAPNVLs
                .Select(x => x.MANHAP)
                .Where(x => x != null && x.StartsWith(prefix))
                .ToList();

            int maxNum = 0;

            foreach (var code in codes)
            {
                var digits = new string(
                    code
                        .Skip(prefix.Length)
                        .TakeWhile(char.IsDigit)
                        .ToArray()
                );

                if (int.TryParse(digits, out int n) && n > maxNum)
                    maxNum = n;
            }

            int next = maxNum + 1;
            string result = $"{prefix}{next:D3}";

            while (codes.Contains(result))
            {
                next++;
                result = $"{prefix}{next:D3}";
            }

            return result;
        }


        private string GenerateMavc()
        {
            const string prefix = "VC";

            var codes = db.VANCHUYENNVLs
                .Select(x => x.MAVC)
                .Where(x => x != null && x.StartsWith(prefix))
                .ToList();

            int maxNum = 0;

            foreach (var code in codes)
            {
                var digits = new string(
                    code
                        .Skip(prefix.Length)
                        .TakeWhile(char.IsDigit)
                        .ToArray()
                );

                if (int.TryParse(digits, out int n) && n > maxNum)
                    maxNum = n;
            }

            int next = maxNum + 1;
            string result = $"{prefix}{next:D3}";

            while (codes.Contains(result))
            {
                next++;
                result = $"{prefix}{next:D3}";
            }

            return result;
        }


        public ActionResult Transport(
            int page = 1,
            string status = "",
            string search = "",
            DateTime? from = null,
            DateTime? to = null)
        {
            try
            {
                string userId = Session["UserId"] as string;
                var st = SettingsHelper.BuildViewBagData(db, userId);
                ViewBag.Settings = st;
                ViewBag.CurrentLang = st.Lang;

                const int pageSize = 10;

                var q = from cp in db.CPDUANs
                        join vc in db.VANCHUYENNVLs
                            on cp.MAVC equals vc.MAVC into vcJoin
                        from vc in vcJoin.DefaultIfEmpty()
                        join nh in db.NHAPNVLs
                            on cp.MANHAP equals nh.MANHAP into nhJoin
                        from nh in nhJoin.DefaultIfEmpty()
                        where vc != null && (vc.CPVC ?? 0) > 0
                        select new { cp, vc, nh };

                if (from.HasValue)
                    q = q.Where(x => x.vc.NGAYVC >= from.Value);
                if (to.HasValue)
                    q = q.Where(x => x.vc.NGAYVC <= to.Value);

                if (!string.IsNullOrWhiteSpace(status))
                {
                    if (status == "done")
                        q = q.Where(x => x.cp.CHIPHITONG.HasValue);
                    else if (status == "pending")
                        q = q.Where(x => !x.cp.CHIPHITONG.HasValue);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    string s = search.Trim();

                    q = q.Where(x =>
                        (x.cp.MAVC ?? "").Contains(s) ||
                        (x.cp.MADA ?? "").Contains(s) ||
                        (x.nh.TENNVL ?? "").Contains(s));
                }

                int totalCount = q.Count();

                decimal totalCost = q
                    .Select(x => (decimal?)(x.cp.CHIPHITONG ?? x.vc.CPVC ?? 0m))
                    .Sum() ?? 0m;

                var data = q
                    .OrderByDescending(x => x.vc.NGAYVC)
                    .ThenByDescending(x => x.cp.MAVC)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var rows = data.Select(x =>
                {
                    decimal cost = x.vc?.CPVC ?? x.cp.CHIPHITONG ?? 0m;

                    return new TransportExpenseRowVM
                    {
                        TransportCode = x.cp.MAVC,
                        TransportDate = x.vc?.NGAYVC,
                        MaterialName = x.nh?.TENNVL,
                        ProjectCode = x.cp.MADA,
                        Cost = cost,
                        StatusCode = x.cp.CHIPHITONG.HasValue ? "done" : "pending"
                    };
                }).ToList();

                var vm = new TransportExpenseViewModel
                {
                    Items = rows,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalCost = totalCost,
                    StatusFilter = status,
                    Search = search,
                    From = from,
                    To = to
                };

                return View(vm);
            }
            catch (Exception)
            {
                TempData["FN_Error"] = "Unable to load transport expenses list.";
                return View(new TransportExpenseViewModel
                {
                    Items = new List<TransportExpenseRowVM>(),
                    Page = 1,
                    PageSize = 10,
                    TotalCount = 0,
                    TotalCost = 0,
                    StatusFilter = "",
                    Search = "",
                    From = null,
                    To = null
                });
            }
        }

        [HttpGet]
        public ActionResult ExportTransport(DateTime? from, DateTime? to)
        {
            try
            {
                var q = db.VANCHUYENNVLs
                          .Include(v => v.CPDUANs.Select(c => c.NHAPNVL))
                          .Where(v => (v.CPVC ?? 0) > 0);

                if (from.HasValue)
                    q = q.Where(v => v.NGAYVC >= from.Value);

                if (to.HasValue)
                    q = q.Where(v => v.NGAYVC <= to.Value);

                var data = q
                    .OrderBy(v => v.NGAYVC)
                    .ThenBy(v => v.MAVC)
                    .ToList();

                using (var wb = new XLWorkbook())
                {
                    var ws = wb.Worksheets.Add("VC_NVL");

                    ws.Cell(1, 1).Value = "Transport date";
                    ws.Cell(1, 2).Value = "Transport code";
                    ws.Cell(1, 3).Value = "Material";
                    ws.Cell(1, 4).Value = "Project code";
                    ws.Cell(1, 5).Value = "Transport cost";

                    int row = 2;
                    foreach (var v in data)
                    {
                        var firstCp = v.CPDUANs.FirstOrDefault();

                        string materialName = null;
                        string projectCode = null;

                        if (firstCp != null)
                        {
                            projectCode = firstCp.MADA;
                            if (firstCp.NHAPNVL != null)
                            {
                                materialName = firstCp.NHAPNVL.TENNVL;
                            }
                        }

                        ws.Cell(row, 1).Value = v.NGAYVC;
                        ws.Cell(row, 1).Style.DateFormat.Format = "dd/MM/yyyy";

                        ws.Cell(row, 2).Value = v.MAVC;
                        ws.Cell(row, 3).Value = materialName ?? "";
                        ws.Cell(row, 4).Value = projectCode ?? "";

                        decimal cost = v.CPVC ?? 0m;
                        ws.Cell(row, 5).Value = cost;
                        ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";

                        row++;
                    }

                    ws.Cell(row, 4).Value = "Total";
                    ws.Cell(row, 4).Style.Font.Bold = true;

                    ws.Cell(row, 5).FormulaA1 = $"SUM(E2:E{row - 1})";
                    ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
                    ws.Cell(row, 5).Style.Font.Bold = true;

                    ws.Columns().AdjustToContents();

                    var now = DateTime.Now;

                    string serverFileName = FileHelper.BuildReportFileName("BCTC", now, "xlsx");
                    string serverFolder = Server.MapPath("~/Content/Uploads/BaoCao");
                    if (!Directory.Exists(serverFolder))
                        Directory.CreateDirectory(serverFolder);

                    string serverPath = Path.Combine(serverFolder, serverFileName);
                    wb.SaveAs(serverPath);

                    using (var stream = new MemoryStream())
                    {
                        wb.SaveAs(stream);
                        stream.Position = 0;

                        string downloadName = $"TransportExpenses_{now:yyyyMMddHHmmss}.xlsx";

                        return File(stream.ToArray(),
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            downloadName);
                    }
                }
            }
            catch (Exception)
            {
                TempData["FN_Error"] = "Unable to export transport expenses file.";
                return new HttpStatusCodeResult(500, "Error exporting transport expenses.");
            }
        }
    }
}
