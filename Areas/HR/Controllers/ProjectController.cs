using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using QLNSVATC.Models;
using QLNSVATC.Helpers;
using QLNSVATC.Areas.HR.Data;
using QLNSVATC.Helper;

namespace QLNSVATC.Areas.HR.Controllers
{
    public class ProjectController : Controller
    {
        private readonly QLNSVATCEntities db = new QLNSVATCEntities();
        private static readonly Random _rnd = new Random();

        public ActionResult Index()
        {
            return RedirectToAction("Information");
        }

        public ActionResult Information()
        {
            if (!CheckAccess.Role("HR"))
            {
                Session.Clear();
                return RedirectToAction("Login", "Account", new { area = "" });
            }
            string userId = Session["UserId"] as string;
            var st = SettingsHelper.BuildViewBagData(db, userId);
            ViewBag.Settings = st;
            ViewBag.CurrentLang = st.Lang;

            DateTime today = DateTime.Today;

            var raw = (from dthd in db.DUANTHEOHOPDONGs
                       join duan in db.DUANs on dthd.MADA equals duan.MADA
                       join hd in db.HOPDONGs on dthd.MAHD equals hd.MAHD
                       join kh in db.KHACHHANGs on duan.MAKH equals kh.MAKH into khJoin
                       from kh in khJoin.DefaultIfEmpty()
                       join dt in db.DTDUANs on duan.MADA equals dt.MADA into dtJoin
                       from dt in dtJoin.DefaultIfEmpty()
                       join kv in db.DTTHEOKVs on dt.MAKV equals kv.MAKV into kvJoin
                       from kv in kvJoin.DefaultIfEmpty()
                       select new
                       {
                           Dthd = dthd,
                           Duan = duan,
                           Hopdong = hd,
                           KhachHang = kh,
                           Area = kv
                       }).ToList();

            var empCountDict = db.NVTHAMGIADAs
                .GroupBy(x => x.MADAHD)
                .Select(g => new { MADAHD = g.Key, Count = g.Count() })
                .ToDictionary(x => x.MADAHD, x => x.Count);

            var allItems = raw.Select(x =>
            {
                var start = x.Hopdong.NGAYBD;
                var end = x.Dthd.NGAYKT;

                var statusCode = GetStatusCode(start, end, today);
                var statusLabel = GetStatusLabel(statusCode);

                int empCount = 0;
                empCountDict.TryGetValue(x.Dthd.MADAHD, out empCount);

                return new
                {
                    TypeCode = (x.Hopdong.LOAI ?? "").Trim(),
                    Item = new ProjectItemViewModel
                    {
                        ProjectKey = x.Dthd.MADAHD,
                        ProjectName = x.Hopdong.TENHD ?? x.Dthd.MADAHD,
                        ProjectCode = x.Duan.MADA,
                        ContractCode = x.Hopdong.MAHD,
                        CustomerName = x.KhachHang != null ? x.KhachHang.LOAIKH : "",
                        AreaCode = x.Area != null ? x.Area.MAKV : null,
                        AreaName = x.Area != null ? x.Area.TENTINH : "",
                        StartDate = start,
                        EndDate = end,
                        EmployeeCount = empCount,
                        StatusCode = statusCode,
                        StatusLabel = statusLabel
                    }
                };
            }).ToList();

            var typeCodes = new[] { "1", "2", "3", "4" };
            var sections = new List<ProjectSectionViewModel>();

            foreach (var t in typeCodes)
            {
                var groupItems = allItems
                    .Where(x => x.TypeCode == t)
                    .Select(x => x.Item)
                    .OrderBy(x => x.ProjectName)
                    .ToList();

                sections.Add(new ProjectSectionViewModel
                {
                    TypeCode = t,
                    TypeName = GetTypeNameByLoai(t),
                    Items = groupItems
                });
            }

            var areaList = db.DTTHEOKVs
                .OrderBy(x => x.TENTINH)
                .ToList();

            var loaiHinhList = db.DTTHEOLHCTs
                .OrderBy(x => x.TENLH)
                .ToList();

            var customerList = db.KHACHHANGs
                .OrderBy(x => x.MAKH)
                .ToList();

            var model = new ProjectInformationViewModel
            {
                Sections = sections,
                Areas = areaList,
                Types = loaiHinhList,
                Customers = customerList
            };

            return View("Information", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateProject(
            string typeCode,
            string projectName,
            string customerId,
            string areaCode,
            string loaiHinhCode,
            DateTime? startDate,
            DateTime? endDate,
            decimal? deposit,
            decimal? expectedTotal,
            decimal? coefficient,
            decimal? finalTotal,
            string note)
        {
            typeCode = (typeCode ?? "").Trim();
            projectName = (projectName ?? "").Trim();
            customerId = (customerId ?? "").Trim();
            areaCode = (areaCode ?? "").Trim();
            loaiHinhCode = (loaiHinhCode ?? "").Trim();

            if (string.IsNullOrWhiteSpace(typeCode) ||
                string.IsNullOrWhiteSpace(projectName) ||
                string.IsNullOrWhiteSpace(customerId) ||
                string.IsNullOrWhiteSpace(areaCode) ||
                string.IsNullOrWhiteSpace(loaiHinhCode))
            {
                return Json(new { success = false, message = "Missing required data." });
            }

            try
            {
                string contractCode = GenerateContractCode();                 // MAHD: 5 ký tự
                string projectCode = BuildProjectCode(typeCode, contractCode); // MADA: {type}+MAHD (6 ký tự)
                string projectKey = projectCode;                               // MADAHD dùng luôn MADA

                using (var tran = db.Database.BeginTransaction())
                {
                    try
                    {
                        var hopdong = new HOPDONG
                        {
                            MAHD = contractCode,
                            TENHD = projectName,
                            LOAI = typeCode,
                            NGAYBD = startDate,
                            NGAYKT_DUTINH = endDate
                        };
                        db.HOPDONGs.Add(hopdong);

                        var duan = new DUAN
                        {
                            MADA = projectCode,
                            MAKH = customerId,
                            TIENCOC = deposit,
                            TIENNGHIEMTHU_DUTINH = expectedTotal,
                        };
                        db.DUANs.Add(duan);

                        var dthd = new DUANTHEOHOPDONG
                        {
                            MADAHD = projectKey,
                            MADA = projectCode,
                            MAHD = contractCode,
                            NGAYKT = endDate,
                        };
                        db.DUANTHEOHOPDONGs.Add(dthd);

                        var dt = new DTDUAN
                        {
                            MADA = projectCode,
                            MAKV = areaCode,
                            MALH = loaiHinhCode,
                            HSTHAYDOI = coefficient,
                            TIENNGHIEMTHU_TONG = finalTotal
                        };
                        db.DTDUANs.Add(dt);

                        db.SaveChanges();
                        tran.Commit();

                        var redirectUrl = Url.Action("Details", "Project",
                            new { area = "HR", id = projectKey });

                        return Json(new
                        {
                            success = true,
                            message = "Project created successfully.",
                            redirectUrl = redirectUrl
                        });
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();

                        var errMsg = ex.InnerException != null
                            ? (ex.InnerException.InnerException != null
                                ? ex.InnerException.InnerException.Message
                                : ex.InnerException.Message)
                            : ex.Message;

                        System.Diagnostics.Debug.WriteLine("CreateProject error: " + ex);

                        return Json(new
                        {
                            success = false,
                            message = "System error while creating project: " + errMsg
                        });
                    }
                }
            }
            catch (Exception exOuter)
            {
                var errMsg = exOuter.InnerException != null
                    ? (exOuter.InnerException.InnerException != null
                        ? exOuter.InnerException.InnerException.Message
                        : exOuter.InnerException.Message)
                    : exOuter.Message;

                System.Diagnostics.Debug.WriteLine("CreateProject outer error: " + exOuter);

                return Json(new
                {
                    success = false,
                    message = "System error while creating project: " + errMsg
                });
            }
        }

        public ActionResult Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return HttpNotFound();
            }

            string userId = Session["UserId"] as string;
            var st = SettingsHelper.BuildViewBagData(db, userId);
            ViewBag.Settings = st;
            ViewBag.CurrentLang = st.Lang;

            var project = db.DUANTHEOHOPDONGs
                .Include(x => x.DUAN)
                .Include(x => x.HOPDONG)
                .FirstOrDefault(x => x.MADAHD == id);

            if (project == null)
            {
                return HttpNotFound();
            }

            var duan = project.DUAN;
            var hopdong = project.HOPDONG;
            var kh = (duan != null && duan.MAKH != null)
                ? db.KHACHHANGs.FirstOrDefault(x => x.MAKH == duan.MAKH)
                : null;

            DateTime today = DateTime.Today;

            var currentEmp = (from link in db.NVTHAMGIADAs
                              join nv in db.NHANVIENs
                                  on link.MANV equals nv.MANV
                              where link.MADAHD == id
                              orderby nv.HOLOT, nv.TENNV
                              select new EmployeeInProjectItem
                              {
                                  EmployeeId = nv.MANV,
                                  FullName = (nv.HOLOT ?? "") + " " + (nv.TENNV ?? "")
                              }).ToList();

            var currentIds = currentEmp.Select(x => x.EmployeeId).ToList();

            var busyIds = (from link in db.NVTHAMGIADAs
                           join dthd in db.DUANTHEOHOPDONGs
                               on link.MADAHD equals dthd.MADAHD
                           where dthd.NGAYKT >= today
                           select link.MANV)
                           .Distinct()
                           .ToList();

            var availableEmp = db.NHANVIENs
                .Where(nv => !busyIds.Contains(nv.MANV) && !currentIds.Contains(nv.MANV))
                .OrderBy(nv => nv.HOLOT)
                .ThenBy(nv => nv.TENNV)
                .Select(nv => new EmployeeOptionItem
                {
                    EmployeeId = nv.MANV,
                    FullName = (nv.HOLOT ?? "") + " " + (nv.TENNV ?? "")
                })
                .ToList();

            var start = hopdong != null ? (DateTime?)hopdong.NGAYBD : null;
            var end = project.NGAYKT;
            var statusCode = GetStatusCode(start, end, today);
            var statusLabel = GetStatusLabel(statusCode);

            var vm = new ProjectDetailsViewModel
            {
                ProjectKey = project.MADAHD,
                ProjectCode = project.MADA,
                ContractCode = project.MAHD,
                ProjectName = hopdong != null ? hopdong.TENHD : project.MADAHD,
                CustomerName = kh != null ? kh.LOAIKH : "",
                StartDate = start,
                EndDate = end,
                StatusCode = statusCode,
                StatusLabel = statusLabel,
                CurrentEmployees = currentEmp,
                AvailableEmployees = availableEmp
            };

            return View("Details", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteProject(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return Json(new { success = false, message = "Project id is missing." });
            }

            using (var tran = db.Database.BeginTransaction())
            {
                try
                {
                    var project = db.DUANTHEOHOPDONGs.FirstOrDefault(x => x.MADAHD == id);
                    if (project == null)
                    {
                        tran.Rollback();
                        return Json(new { success = false, message = "Project not found." });
                    }

                    string maDa = project.MADA;
                    string maHd = project.MAHD;

                    var joinEmp = db.NVTHAMGIADAs.Where(x => x.MADAHD == id).ToList();
                    if (joinEmp.Any())
                    {
                        db.NVTHAMGIADAs.RemoveRange(joinEmp);
                    }

                    if (!string.IsNullOrEmpty(maDa))
                    {
                        var cpRows = db.CPDUANs.Where(x => x.MADA == maDa).ToList();
                        if (cpRows.Any())
                            db.CPDUANs.RemoveRange(cpRows);

                        var dtRows = db.DTDUANs.Where(x => x.MADA == maDa).ToList();
                        if (dtRows.Any())
                            db.DTDUANs.RemoveRange(dtRows);
                    }

                    db.DUANTHEOHOPDONGs.Remove(project);

                    if (!string.IsNullOrEmpty(maDa))
                    {
                        var duan = db.DUANs.FirstOrDefault(x => x.MADA == maDa);
                        if (duan != null)
                        {
                            db.DUANs.Remove(duan);
                        }
                    }

                    if (!string.IsNullOrEmpty(maHd))
                    {
                        bool anyOther = db.DUANTHEOHOPDONGs.Any(x => x.MAHD == maHd);
                        if (!anyOther)
                        {
                            var hop = db.HOPDONGs.FirstOrDefault(x => x.MAHD == maHd);
                            if (hop != null) db.HOPDONGs.Remove(hop);
                        }
                    }

                    db.SaveChanges();
                    tran.Commit();

                    return Json(new { success = true, message = "Project deleted successfully." });
                }
                catch (Exception ex)
                {
                    tran.Rollback();

                    var errMsg = ex.InnerException != null
                        ? (ex.InnerException.InnerException != null
                            ? ex.InnerException.InnerException.Message
                            : ex.InnerException.Message)
                        : ex.Message;

                    System.Diagnostics.Debug.WriteLine("DeleteProject error: " + ex);

                    return Json(new
                    {
                        success = false,
                        message = "Cannot delete this project: " + errMsg
                    });
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddEmployee(string projectId, string employeeId)
        {
            if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(employeeId))
            {
                return Json(new { success = false, message = "Invalid data." });
            }

            DateTime today = DateTime.Today;

            var project = db.DUANTHEOHOPDONGs.FirstOrDefault(x => x.MADAHD == projectId);
            if (project == null)
            {
                return Json(new { success = false, message = "Project not found." });
            }

            bool exists = db.NVTHAMGIADAs.Any(x => x.MADAHD == projectId && x.MANV == employeeId);
            if (exists)
            {
                return Json(new { success = false, message = "Employee already in this project." });
            }

            bool busy = (from link in db.NVTHAMGIADAs
                         join dthd in db.DUANTHEOHOPDONGs
                             on link.MADAHD equals dthd.MADAHD
                         where link.MANV == employeeId && dthd.NGAYKT >= today
                         select link).Any();

            if (busy)
            {
                return Json(new
                {
                    success = false,
                    message = "Employee is assigned to another active project."
                });
            }

            var record = new NVTHAMGIADA
            {
                MADAHD = projectId,
                MANV = employeeId,
                TONGTGLAMVIEC = null,
                LUONGTHEODUAN = null
            };

            db.NVTHAMGIADAs.Add(record);
            db.SaveChanges();

            return Json(new { success = true, message = "Employee added to project." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveEmployee(string projectId, string employeeId)
        {
            if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(employeeId))
            {
                return Json(new { success = false, message = "Invalid data." });
            }

            var link = db.NVTHAMGIADAs.FirstOrDefault(x => x.MADAHD == projectId && x.MANV == employeeId);
            if (link == null)
            {
                return Json(new { success = false, message = "Record not found." });
            }

            db.NVTHAMGIADAs.Remove(link);
            db.SaveChanges();

            return Json(new { success = true, message = "Employee removed from project." });
        }

        private string GetStatusCode(DateTime? start, DateTime? end, DateTime today)
        {
            if (end.HasValue && end.Value < today)
            {
                return "done";
            }
            if (start.HasValue && start.Value > today)
            {
                return "upcoming";
            }
            if (end.HasValue && end.Value >= today)
            {
                return "ongoing";
            }
            return "ongoing";
        }

        private string GetStatusLabel(string code)
        {
            switch ((code ?? "").ToLower())
            {
                case "upcoming": return "Upcoming";
                case "ongoing": return "Ongoing";
                case "done": return "Done";
                case "overdue": return "Overdue";
            }
            return "On track";
        }

        private string GetTypeNameByLoai(string loai)
        {
            switch ((loai ?? "").Trim())
            {
                case "1": return "Type 1";
                case "2": return "Type 2";
                case "3": return "Type 3";
                case "4": return "Type 4";
                default: return "Other";
            }
        }

        // MADA = {typeCode} + MAHD (5 ký tự)
        private string BuildProjectCode(string loai, string contractCode)
        {
            loai = (loai ?? "").Trim();
            if (string.IsNullOrEmpty(loai)) loai = "0";
            if (loai.Length > 1) loai = loai.Substring(0, 1);

            contractCode = (contractCode ?? "").Trim();
            return loai + contractCode;
        }

        private string GenerateContractCode()
        {
            string code;
            do
            {
                code = RandomLetters(3) + RandomDigits(2);
            }
            while (db.HOPDONGs.Any(x => x.MAHD == code));

            return code;
        }

        private string RandomLetters(int length)
        {
            const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var chars = new char[length];
            for (int i = 0; i < length; i++)
            {
                chars[i] = letters[_rnd.Next(letters.Length)];
            }
            return new string(chars);
        }

        private string RandomDigits(int length)
        {
            const string digits = "0123456789";
            var chars = new char[length];
            for (int i = 0; i < length; i++)
            {
                chars[i] = digits[_rnd.Next(digits.Length)];
            }
            return new string(chars);
        }
    }
}
