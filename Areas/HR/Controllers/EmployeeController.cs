using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Drawing.Printing;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using QLNSVATC.Helpers;
using QLNSVATC.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;
using iTextFont = iTextSharp.text.Font;
using QLNSVATC.Helper;

namespace QLNSVATC.Areas.HR.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly QLNSVATCEntities db = new QLNSVATCEntities();

        public ActionResult Information(string keyword, string maPB, string maCV, string loaiNV)
        {
            try
            {
                if (!CheckAccess.Role("HR"))
                {
                    Session.Clear();
                    return RedirectToAction("Login", "Account", new { area = "" });
                }

                var userId = Session["UserId"] as string;
                var st = SettingsHelper.BuildViewBagData(db, userId);
                ViewBag.Settings = st;

                var query = db.NHANVIENs
                              .Include("PHONGBAN")
                              .Include("VITRICONGVIEC")
                              .Include("LUONG")
                              .AsQueryable();

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    keyword = keyword.Trim();
                    query = query.Where(nv =>
                        nv.MANV.Contains(keyword) ||
                        (nv.HOLOT + " " + nv.TENNV).Contains(keyword));
                }

                if (!string.IsNullOrWhiteSpace(maPB))
                {
                    query = query.Where(nv => nv.MAPB == maPB);
                }

                if (!string.IsNullOrWhiteSpace(maCV))
                {
                    query = query.Where(nv => nv.MACV == maCV);
                }

                if (!string.IsNullOrWhiteSpace(loaiNV))
                {
                    query = query.Where(nv => nv.LUONG != null && nv.LUONG.LOAINV == loaiNV);
                }

                var employees = query
                    .OrderBy(nv => nv.MANV)
                    .ToList();

                ViewBag.Departments = db.PHONGBANs
                    .OrderBy(p => p.TENPB)
                    .ToList();

                ViewBag.Positions = db.VITRICONGVIECs
                    .OrderBy(c => c.TENCV)
                    .ToList();

                ViewBag.LoaiNVs = db.LUONGs
                    .Where(l => l.LOAINV != null && l.LOAINV != "")
                    .Select(l => l.LOAINV)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                return View(employees);
            }
            catch (Exception)
            {
                TempData["EmployeeError"] = "System error while loading employee list.";
                ViewBag.Departments = new List<PHONGBAN>();
                ViewBag.Positions = new List<VITRICONGVIEC>();
                ViewBag.LoaiNVs = new List<string>();
                return View(new List<NHANVIEN>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return Json(new { success = false, message = "Invalid employee id." });

            id = id.Trim();

            var nv = db.NHANVIENs.FirstOrDefault(x => x.MANV == id);
            if (nv == null)
                return Json(new { success = false, message = "Employee not found." });

            using (var tran = db.Database.BeginTransaction())
            {
                try
                {
                    var deptHeadList = db.PHONGBANs
                        .Where(p => p.MATRG_PHG == id)
                        .ToList();

                    foreach (var pb in deptHeadList)
                    {
                        pb.MATRG_PHG = null;
                    }

                    var chamCong = db.CHAMCONGs.Where(x => x.MANV == id);
                    db.CHAMCONGs.RemoveRange(chamCong);

                    var thuongPhat = db.DSTHUONGPHATs.Where(x => x.MANV == id);
                    db.DSTHUONGPHATs.RemoveRange(thuongPhat);

                    var lichLamViec = db.LICHLAMVIECs.Where(x => x.MANV == id);
                    db.LICHLAMVIECs.RemoveRange(lichLamViec);

                    var luong = db.LUONGs.Where(x => x.MANV == id);
                    db.LUONGs.RemoveRange(luong);

                    var nhanThan = db.NHANTHANs.Where(x => x.MANV == id);
                    db.NHANTHANs.RemoveRange(nhanThan);

                    var nvDuAn = db.NVTHAMGIADAs.Where(x => x.MANV == id);
                    db.NVTHAMGIADAs.RemoveRange(nvDuAn);

                    var baoHiem = db.THONGTINBAOHIEMs.Where(x => x.MANV == id);
                    db.THONGTINBAOHIEMs.RemoveRange(baoHiem);

                    var lienHe = db.THONGTINLIENHEs.Where(x => x.MANV == id);
                    db.THONGTINLIENHEs.RemoveRange(lienHe);

                    var sucKhoe = db.THONGTINSUCKHOEs.Where(x => x.MANV == id);
                    db.THONGTINSUCKHOEs.RemoveRange(sucKhoe);

                    db.NHANVIENs.Remove(nv);

                    db.SaveChanges();
                    tran.Commit();

                    return Json(new
                    {
                        success = true,
                        message = "Employee and all related records have been deleted successfully."
                    });
                }
                catch (Exception)
                {
                    tran.Rollback();

                    return Json(new
                    {
                        success = false,
                        message = "System error while deleting this employee. Please try again later."
                    });
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdatePosition(string id, string maPB, string maCV)
        {
            if (string.IsNullOrWhiteSpace(id))
                return Json(new { success = false, message = "Invalid employee id." });

            var nv = db.NHANVIENs.Find(id);
            if (nv == null)
                return Json(new { success = false, message = "Employee not found." });

            try
            {
                nv.MAPB = maPB;
                nv.MACV = maCV;
                db.SaveChanges();

                return Json(new { success = true, message = "Position has been updated." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "System error while updating position." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddReward(string id, decimal amount, string note)
        {
            if (string.IsNullOrWhiteSpace(id))
                return Json(new { success = false, message = "Invalid employee id." });

            var nv = db.NHANVIENs.Find(id);
            if (nv == null)
                return Json(new { success = false, message = "Employee not found." });

            if (amount <= 0)
                return Json(new { success = false, message = "Amount must be greater than 0." });

            try
            {
                var item = new DSTHUONGPHAT
                {
                    MANV = id,
                    NGAY = DateTime.Now,
                    HINHTHUC = "KT",
                    SOTIEN = amount,
                    NOTE = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
                };

                db.DSTHUONGPHATs.Add(item);
                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Reward has been saved successfully."
                });
            }
            catch (DbUpdateException ex)
            {
                var inner = ex.InnerException;
                while (inner != null && inner.InnerException != null)
                {
                    inner = inner.InnerException;
                }

                var detail = inner != null ? inner.Message : ex.Message;

                return Json(new
                {
                    success = false,
                    message = "System error when saving reward: " + detail
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Unexpected error when saving reward: " + ex.Message
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddPenalty(string id, decimal amount, string note)
        {
            if (string.IsNullOrWhiteSpace(id))
                return Json(new { success = false, message = "Invalid employee id." });

            var nv = db.NHANVIENs.Find(id);
            if (nv == null)
                return Json(new { success = false, message = "Employee not found." });

            if (amount <= 0)
                return Json(new { success = false, message = "Amount must be greater than 0." });

            try
            {
                var item = new DSTHUONGPHAT
                {
                    MANV = id,
                    NGAY = DateTime.Now,
                    HINHTHUC = "KL",
                    SOTIEN = amount,
                    NOTE = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
                };

                db.DSTHUONGPHATs.Add(item);
                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Penalty has been saved successfully."
                });
            }
            catch (DbUpdateException ex)
            {
                var inner = ex.InnerException;
                while (inner != null && inner.InnerException != null)
                {
                    inner = inner.InnerException;
                }

                var detail = inner != null ? inner.Message : ex.Message;

                return Json(new
                {
                    success = false,
                    message = "System error when saving penalty: " + detail
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Unexpected error when saving penalty: " + ex.Message
                });
            }
        }

        public ActionResult Create()
        {
            try
            {
                var userId = Session["UserId"] as string;
                var st = SettingsHelper.BuildViewBagData(db, userId);
                ViewBag.Settings = st;

                ViewBag.Departments = db.PHONGBANs
                    .OrderBy(p => p.TENPB)
                    .ToList();

                ViewBag.Positions = db.VITRICONGVIECs
                    .OrderBy(c => c.TENCV)
                    .ToList();

                ViewBag.LoaiNVs = db.LUONGs
                    .Where(l => l.LOAINV != null && l.LOAINV != "")
                    .Select(l => l.LOAINV)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                var employees = db.NHANVIENs
                    .Include("PHONGBAN")
                    .Include("VITRICONGVIEC")
                    .Include("LUONG")
                    .OrderBy(nv => nv.MANV)
                    .ToList();

                return View("Information", employees);
            }
            catch (Exception)
            {
                TempData["CreateError"] = "System error while loading employee creation view.";
                ViewBag.Departments = new List<PHONGBAN>();
                ViewBag.Positions = new List<VITRICONGVIEC>();
                ViewBag.LoaiNVs = new List<string>();
                return View("Information", new List<NHANVIEN>());
            }
        }

        public static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            string normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (char c in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private string GenerateEmployeeCode(NHANVIEN model)
        {
            var rnd = new Random();

            string name = (model.TENNV ?? "").Trim();
            name = RemoveDiacritics(name).ToUpper().Replace(" ", "");

            if (string.IsNullOrEmpty(name))
            {
                name = "NV";
            }

            string namePart = name.Length >= 4 ? name.Substring(0, 4) : name;

            while (namePart.Length < 4)
            {
                char c = (char)rnd.Next('A', 'Z' + 1);
                namePart += c;
            }

            int year = model.NAMSINH ?? DateTime.Now.Year;
            int yearTwo = year % 100;
            string yearPart = yearTwo.ToString("00");
            bool gioiTinh = model.GIOITINH ?? true;
            string genderPart = gioiTinh ? "11" : "00";
            string randPart = "";
            for (int i = 0; i < 2; i++)
            {
                char c = (char)rnd.Next('A', 'Z' + 1);
                randPart += c;
            }

            string baseCode = namePart + yearPart + genderPart + randPart;
            string finalCode = baseCode;
            int counter = 0;
            while (db.NHANVIENs.Any(x => x.MANV == finalCode) && counter < 100)
            {
                finalCode = baseCode.Substring(0, 8) + counter.ToString("00");
                counter++;
            }

            return finalCode;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(NHANVIEN model, HttpPostedFileBase PhotoFile)
        {
            var userId = Session["UserId"] as string;
            var st = SettingsHelper.BuildViewBagData(db, userId);
            ViewBag.Settings = st;

            if (string.IsNullOrWhiteSpace(model.HOLOT))
            {
                ModelState.AddModelError("HOLOT", "Please enter last / middle name.");
            }

            if (string.IsNullOrWhiteSpace(model.TENNV))
            {
                ModelState.AddModelError("TENNV", "Please enter employee first name.");
            }

            if (!model.GIOITINH.HasValue)
            {
                ModelState.AddModelError("GIOITINH", "Please select gender.");
            }

            if (!model.NAMSINH.HasValue)
            {
                ModelState.AddModelError("NAMSINH", "Please enter birth year.");
            }
            else if (model.NAMSINH < 1950 || model.NAMSINH > DateTime.Now.Year)
            {
                ModelState.AddModelError("NAMSINH", "Birth year is not valid.");
            }

            if (string.IsNullOrWhiteSpace(model.MAPB))
            {
                ModelState.AddModelError("MAPB", "Please select department.");
            }

            if (string.IsNullOrWhiteSpace(model.MACV))
            {
                ModelState.AddModelError("MACV", "Please select position.");
            }

            if (PhotoFile == null || PhotoFile.ContentLength == 0)
            {
                ModelState.AddModelError("PhotoFile", "Please choose an employee photo.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Departments = db.PHONGBANs
                    .OrderBy(p => p.TENPB)
                    .ToList();

                ViewBag.Positions = db.VITRICONGVIECs
                    .OrderBy(c => c.TENCV)
                    .ToList();

                ViewBag.LoaiNVs = db.LUONGs
                    .Where(l => l.LOAINV != null && l.LOAINV != "")
                    .Select(l => l.LOAINV)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                var employeesListInvalid = db.NHANVIENs
                    .Include("PHONGBAN")
                    .Include("VITRICONGVIEC")
                    .Include("LUONG")
                    .OrderBy(nv => nv.MANV)
                    .ToList();

                ViewBag.OpenCreateModal = true;
                ViewBag.CreateModel = model;
                TempData["CreateError"] = "Some information is missing or invalid. Please check the form and try again.";

                return View("Information", employeesListInvalid);
            }

            try
            {
                model.MANV = GenerateEmployeeCode(model);

                if (db.NHANVIENs.Any(x => x.MANV == model.MANV))
                {
                    ViewBag.Departments = db.PHONGBANs
                        .OrderBy(p => p.TENPB)
                        .ToList();

                    ViewBag.Positions = db.VITRICONGVIECs
                        .OrderBy(c => c.TENCV)
                        .ToList();

                    ViewBag.LoaiNVs = db.LUONGs
                        .Where(l => l.LOAINV != null && l.LOAINV != "")
                        .Select(l => l.LOAINV)
                        .Distinct()
                        .OrderBy(x => x)
                        .ToList();

                    var employeesList = db.NHANVIENs
                        .Include("PHONGBAN")
                        .Include("VITRICONGVIEC")
                        .Include("LUONG")
                        .OrderBy(nv => nv.MANV)
                        .ToList();

                    ViewBag.OpenCreateModal = true;
                    ViewBag.CreateModel = model;
                    TempData["CreateError"] = "Employee code already exists. Please try again.";

                    return View("Information", employeesList);
                }

                db.NHANVIENs.Add(model);
                db.SaveChanges();

                if (PhotoFile != null && PhotoFile.ContentLength > 0)
                {
                    var folder = Server.MapPath("~/Content/Images/Home/NhanVien");
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    var path = Path.Combine(folder, model.MANV + ".jpg");
                    PhotoFile.SaveAs(path);
                }

                TempData["CreateSuccess"] = "Employee has been created successfully.";
                return RedirectToAction("Information");
            }
            catch (Exception)
            {
                ViewBag.Departments = db.PHONGBANs
                    .OrderBy(p => p.TENPB)
                    .ToList();

                ViewBag.Positions = db.VITRICONGVIECs
                    .OrderBy(c => c.TENCV)
                    .ToList();

                ViewBag.LoaiNVs = db.LUONGs
                    .Where(l => l.LOAINV != null && l.LOAINV != "")
                    .Select(l => l.LOAINV)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                var employeesListError = db.NHANVIENs
                    .Include("PHONGBAN")
                    .Include("VITRICONGVIEC")
                    .Include("LUONG")
                    .OrderBy(nv => nv.MANV)
                    .ToList();

                ViewBag.OpenCreateModal = true;
                ViewBag.CreateModel = model;
                TempData["CreateError"] = "System error while creating employee. Please try again later.";

                return View("Information", employeesListError);
            }
        }

        private static string BuildFolderName(string tenUngVien, string fileName)
        {
            // Dựa trên cách lưu: folder = {yyyyMMddHH}_{TenKhongDauKhongSpace}
            if (string.IsNullOrWhiteSpace(tenUngVien) || string.IsNullOrWhiteSpace(fileName))
                return null;

            var baseName = Path.GetFileNameWithoutExtension(fileName);
            if (string.IsNullOrEmpty(baseName)) return null;

            // lấy chuỗi số đầu tiên dài 14 / 12 / 10
            var m = Regex.Match(baseName, @"\d{14}|\d{12}|\d{10}");
            if (!m.Success) return null;

            string timeStr = m.Value;
            // folder chỉ dùng đến yyyyMMddHH → 10 ký tự đầu
            if (timeStr.Length > 10)
                timeStr = timeStr.Substring(0, 10);

            string name = RemoveDiacritics(tenUngVien ?? "")
                .Trim()
                .Replace(" ", string.Empty);

            if (string.IsNullOrEmpty(name)) name = "Candidate";

            return $"{timeStr}_{name}";
        }

        private static string BuildFileUrl(string tenUngVien, string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return null;

            string folder = BuildFolderName(tenUngVien, fileName);
            if (string.IsNullOrEmpty(folder))
            {
                // fallback: không có folder
                return "/Content/Uploads/HoSoUngVien/" + fileName;
            }

            return "/Content/Uploads/HoSoUngVien/" + folder + "/" + fileName;
        }

        public ActionResult ProfileGet(string id)
        {
            try
            {
                var userId = Session["UserId"] as string;
                var st = SettingsHelper.BuildViewBagData(db, userId);
                ViewBag.Settings = st;

                ViewBag.Departments = db.PHONGBANs
                    .OrderBy(p => p.TENPB)
                    .ToList();

                ViewBag.Positions = db.VITRICONGVIECs
                    .OrderBy(c => c.TENCV)
                    .ToList();

                ViewBag.LoaiNVs = db.LUONGs
                    .Where(l => l.LOAINV != null && l.LOAINV != "")
                    .Select(l => l.LOAINV)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                if (string.IsNullOrWhiteSpace(id))
                {
                    var first = db.NHANVIENs.OrderBy(x => x.MANV).FirstOrDefault();
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

                var lichDauTien = db.LICHLAMVIECs
                    .Where(x => x.MANV == emp.nv.MANV)
                    .OrderBy(x => x.NGAYLAMVIEC)
                    .FirstOrDefault();
                DateTime? ngayBatDau = lichDauTien != null ? lichDauTien.NGAYLAMVIEC : emp.nv.HDLD;

                var fromDate = today.AddMonths(-2).AddDays(1 - today.Day);
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
                    .Where(x => x.HINHTHUC == "KT")
                    .Select(x => (decimal?)x.TONG)
                    .DefaultIfEmpty(0)
                    .Sum();

                decimal? tongPhat = thuongPhatList
                    .Where(x => x.HINHTHUC == "KL")
                    .Select(x => (decimal?)x.TONG)
                    .DefaultIfEmpty(0)
                    .Sum();

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

                var sk = db.THONGTINSUCKHOEs.FirstOrDefault(x => x.MANV == emp.nv.MANV);
                if (sk == null)
                {
                    sk = new THONGTINSUCKHOE
                    {
                        MANV = emp.nv.MANV,
                        NGAYCAPNHAT = DateTime.Today
                    };
                    db.THONGTINSUCKHOEs.Add(sk);
                    db.SaveChanges();
                }

                var bh = db.THONGTINBAOHIEMs.FirstOrDefault(x => x.MANV == emp.nv.MANV);
                if (bh == null)
                {
                    bh = new THONGTINBAOHIEM
                    {
                        MANV = emp.nv.MANV
                    };
                    db.THONGTINBAOHIEMs.Add(bh);
                    db.SaveChanges();
                }

                string queQuanText = emp.tt != null && emp.tt.QUEQUAN.HasValue
                    ? "Area code: " + emp.tt.QUEQUAN.Value
                    : null;

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

                    NgayBatDau = ngayBatDau,
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
                    TongPhat = tongPhat,

                    ChieuCao = sk.CHIEUCAO,
                    CanNang = sk.CANNANG,
                    TienSuBenh = sk.TIENSUBENH,
                    ThiLucTren10 = sk.THILUCTREN10,
                    NgayCapNhatSucKhoe = sk.NGAYCAPNHAT,
                    LoaiBaoHiem = bh.LOAIBAOHIEM,
                    SoBaoHiem = bh.SOBAOHIEM,
                    ThoiHanBaoHiem = bh.THOIHAN
                };

                return View(model);
            }
            catch (Exception)
            {
                TempData["ProfileError"] = "System error while loading profile.";
                return RedirectToAction("Information", "Employee", new { area = "HR" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Profile")]
        public ActionResult SaveProfile(HREmployeeInformationViewModel model)
        {
            var userId = Session["UserId"] as string;
            var st = SettingsHelper.BuildViewBagData(db, userId);
            ViewBag.Settings = st;

            if (model == null || string.IsNullOrWhiteSpace(model.MaNV))
                return RedirectToAction("Information", "Employee", new { area = "HR" });

            try
            {
                var nv = db.NHANVIENs.FirstOrDefault(x => x.MANV == model.MaNV);
                if (nv == null) return HttpNotFound();

                nv.HOLOT = model.HoLot;
                nv.TENNV = model.TenNV;
                nv.GIOITINH = model.GioiTinh;

                if (model.NamSinh.HasValue)
                    nv.NAMSINH = (short?)model.NamSinh.Value;
                else
                    nv.NAMSINH = null;

                nv.MAPB = model.MaPB;
                nv.MACV = model.MaCV;
                nv.MADN = model.MaDN;
                nv.HDLD = model.NgayHDLD ?? nv.HDLD;

                var tt = db.THONGTINLIENHEs.FirstOrDefault(x => x.MANV == model.MaNV);
                if (tt == null)
                {
                    tt = new THONGTINLIENHE { MANV = model.MaNV };
                    db.THONGTINLIENHEs.Add(tt);
                }

                int? queCode = null;
                if (!string.IsNullOrWhiteSpace(model.QueQuanText))
                {
                    int tmp;
                    if (int.TryParse(model.QueQuanText, out tmp))
                        queCode = tmp;
                }

                tt.SODT = model.SDT;
                tt.GMAIL = model.Email;
                tt.DIACHI = model.DiaChi;
                tt.FB = model.Facebook;

                var luong = db.LUONGs.FirstOrDefault(x => x.MANV == model.MaNV);
                if (luong == null)
                {
                    luong = new LUONG { MANV = model.MaNV };
                    db.LUONGs.Add(luong);
                }

                luong.LOAINV = string.IsNullOrWhiteSpace(model.LoaiNV) ? null : model.LoaiNV;
                luong.LUONGCOBAN = model.LuongCoBan;

                var thanNhan = db.NHANTHANs.FirstOrDefault(x => x.MANV == model.MaNV);
                if (thanNhan == null && !string.IsNullOrWhiteSpace(model.NguoiThanTen))
                {
                    thanNhan = new NHANTHAN
                    {
                        MANV = model.MaNV,
                        TENNT = model.NguoiThanTen
                    };
                    db.NHANTHANs.Add(thanNhan);
                }

                if (thanNhan != null)
                {
                    if (!string.IsNullOrWhiteSpace(model.NguoiThanTen))
                        thanNhan.TENNT = model.NguoiThanTen;

                    thanNhan.QUANHE = model.NguoiThanQuanHe;
                    thanNhan.DIENTHOAI = model.NguoiThanSDT;
                    thanNhan.DIACHI = model.NguoiThanDiaChi;
                }

                var sk = db.THONGTINSUCKHOEs.FirstOrDefault(x => x.MANV == model.MaNV);
                if (sk == null)
                {
                    sk = new THONGTINSUCKHOE { MANV = model.MaNV };
                    db.THONGTINSUCKHOEs.Add(sk);
                }

                if (model.ChieuCao.HasValue)
                    sk.CHIEUCAO = (byte?)model.ChieuCao.Value;
                else
                    sk.CHIEUCAO = null;

                if (model.CanNang.HasValue)
                    sk.CANNANG = (byte?)model.CanNang.Value;
                else
                    sk.CANNANG = null;

                sk.TIENSUBENH = model.TienSuBenh;

                if (model.ThiLucTren10.HasValue)
                    sk.THILUCTREN10 = (byte?)model.ThiLucTren10.Value;
                else
                    sk.THILUCTREN10 = null;

                sk.NGAYCAPNHAT = model.NgayCapNhatSucKhoe ?? DateTime.Today;

                var bh = db.THONGTINBAOHIEMs.FirstOrDefault(x => x.MANV == model.MaNV);
                if (bh == null)
                {
                    bh = new THONGTINBAOHIEM { MANV = model.MaNV };
                    db.THONGTINBAOHIEMs.Add(bh);
                }

                bh.LOAIBAOHIEM = model.LoaiBaoHiem;
                bh.SOBAOHIEM = model.SoBaoHiem;
                bh.THOIHAN = model.ThoiHanBaoHiem;

                db.SaveChanges();

                TempData["ProfileSuccess"] = "Profile has been updated.";
                return RedirectToAction("Profile", new { id = model.MaNV });
            }
            catch (Exception)
            {
                TempData["ProfileError"] = "System error while saving profile.";
                return RedirectToAction("Profile", new { id = model.MaNV });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdatePersonal(HREmployeeInformationViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.MaNV))
            {
                return Json(new { success = false, message = "Invalid employee id." });
            }

            var nv = db.NHANVIENs.Find(model.MaNV);
            if (nv == null)
            {
                return Json(new { success = false, message = "Employee not found." });
            }

            try
            {
                nv.HOLOT = model.HoLot;
                nv.TENNV = model.TenNV;
                nv.GIOITINH = model.GioiTinh;
                nv.NAMSINH = model.NamSinh;
                nv.HDLD = model.NgayBatDau;

                var tt = db.THONGTINLIENHEs.FirstOrDefault(x => x.MANV == model.MaNV);
                if (tt == null)
                {
                    tt = new THONGTINLIENHE { MANV = model.MaNV };
                    db.THONGTINLIENHEs.Add(tt);
                }

                db.SaveChanges();

                return Json(new { success = true, message = "Personal information has been updated." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "System error when saving personal information." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateContact(HREmployeeInformationViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.MaNV))
            {
                return Json(new { success = false, message = "Invalid employee id." });
            }

            var tt = db.THONGTINLIENHEs.FirstOrDefault(x => x.MANV == model.MaNV);
            if (tt == null)
            {
                tt = new THONGTINLIENHE { MANV = model.MaNV };
                db.THONGTINLIENHEs.Add(tt);
            }

            try
            {
                tt.SODT = model.SDT;
                tt.GMAIL = model.Email;
                tt.DIACHI = model.DiaChi;
                tt.FB = model.Facebook;

                db.SaveChanges();

                return Json(new { success = true, message = "Contact information has been updated." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "System error when saving contact information." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateOrgSalary(HREmployeeInformationViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.MaNV))
            {
                return Json(new { success = false, message = "Invalid employee id." });
            }

            var nv = db.NHANVIENs.Find(model.MaNV);
            if (nv == null)
            {
                return Json(new { success = false, message = "Employee not found." });
            }

            var luong = db.LUONGs.FirstOrDefault(x => x.MANV == model.MaNV);
            if (luong == null)
            {
                luong = new LUONG { MANV = model.MaNV };
                db.LUONGs.Add(luong);
            }

            try
            {
                nv.MAPB = model.MaPB;
                nv.MACV = model.MaCV;

                luong.LOAINV = model.LoaiNV;
                luong.LUONGCOBAN = model.LuongCoBan;

                db.SaveChanges();

                return Json(new { success = true, message = "Department, position and salary have been updated." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "System error when saving department / position / salary information." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateRelative(HREmployeeInformationViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.MaNV))
            {
                return Json(new { success = false, message = "Invalid employee id." });
            }

            var nt = db.NHANTHANs.FirstOrDefault(x => x.MANV == model.MaNV);
            if (nt == null)
            {
                nt = new NHANTHAN { MANV = model.MaNV };
                db.NHANTHANs.Add(nt);
            }

            try
            {
                nt.TENNT = model.NguoiThanTen;
                nt.QUANHE = model.NguoiThanQuanHe;
                nt.DIENTHOAI = model.NguoiThanSDT;
                nt.DIACHI = model.NguoiThanDiaChi;

                db.SaveChanges();

                return Json(new { success = true, message = "Relative information has been updated." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "System error when saving relative information." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateHealthInsurance(HREmployeeInformationViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.MaNV))
            {
                return Json(new { success = false, message = "Invalid employee id." });
            }

            var sk = db.THONGTINSUCKHOEs.FirstOrDefault(x => x.MANV == model.MaNV);
            if (sk == null)
            {
                sk = new THONGTINSUCKHOE { MANV = model.MaNV };
                db.THONGTINSUCKHOEs.Add(sk);
            }

            var bh = db.THONGTINBAOHIEMs.FirstOrDefault(x => x.MANV == model.MaNV);
            if (bh == null)
            {
                bh = new THONGTINBAOHIEM { MANV = model.MaNV };
                db.THONGTINBAOHIEMs.Add(bh);
            }

            try
            {
                sk.CHIEUCAO = model.ChieuCao;
                sk.CANNANG = model.CanNang;
                sk.TIENSUBENH = string.IsNullOrWhiteSpace(model.TienSuBenh)
                    ? null
                    : model.TienSuBenh;
                sk.THILUCTREN10 = model.ThiLucTren10;
                sk.NGAYCAPNHAT = model.NgayCapNhatSucKhoe ?? DateTime.Today;

                bh.LOAIBAOHIEM = string.IsNullOrWhiteSpace(model.LoaiBaoHiem)
                    ? null
                    : model.LoaiBaoHiem;
                bh.SOBAOHIEM = string.IsNullOrWhiteSpace(model.SoBaoHiem)
                    ? null
                    : model.SoBaoHiem;
                bh.THOIHAN = model.ThoiHanBaoHiem;

                db.SaveChanges();

                return Json(new { success = true, message = "Health and insurance information has been updated." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "System error when saving health / insurance information." });
            }
        }

        public ActionResult Candidate(string keyword, DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                var userId = Session["UserId"] as string;
                var st = SettingsHelper.BuildViewBagData(db, userId);
                ViewBag.Settings = st;

                var query = db.HOSOVIECLAMs.AsQueryable();

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    keyword = keyword.Trim();
                    query = query.Where(x =>
                        x.TENUNGVIEN.Contains(keyword) ||
                        x.EMAIL.Contains(keyword));
                }

                var rawList = query
                    .OrderByDescending(x => x.ID)
                    .ToList();

                var list = rawList
                    .Select(x =>
                    {
                        string mainFile = x.FILETHONGTIN;
                        if (string.IsNullOrWhiteSpace(mainFile))
                        {
                            if (!string.IsNullOrWhiteSpace(x.FILEBANGCAP))
                                mainFile = x.FILEBANGCAP;
                            else if (!string.IsNullOrWhiteSpace(x.FILEKHAC))
                                mainFile = x.FILEKHAC;
                        }

                        DateTime? submittedAt = ExtractDateFromFileName(mainFile);

                        return new CandidateViewModel
                        {
                            ID = x.ID,
                            TenUngVien = x.TENUNGVIEN,
                            Email = x.EMAIL,
                            FileThongTin = x.FILETHONGTIN,
                            FileBangCap = x.FILEBANGCAP,
                            FileKhac = x.FILEKHAC,
                            FileThongTinUrl = BuildFileUrl(x.TENUNGVIEN, x.FILETHONGTIN),
                            FileBangCapUrl = BuildFileUrl(x.TENUNGVIEN, x.FILEBANGCAP),
                            FileKhacUrl = BuildFileUrl(x.TENUNGVIEN, x.FILEKHAC),
                            SubmittedAt = submittedAt
                        };
                    })
                    .ToList();

                if (fromDate.HasValue)
                {
                    var from = fromDate.Value.Date;
                    list = list
                        .Where(c => c.SubmittedAt.HasValue &&
                                    c.SubmittedAt.Value.Date >= from)
                        .ToList();
                }

                if (toDate.HasValue)
                {
                    var to = toDate.Value.Date;
                    list = list
                        .Where(c => c.SubmittedAt.HasValue &&
                                    c.SubmittedAt.Value.Date <= to)
                        .ToList();
                }

                ViewBag.Keyword = keyword;
                ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
                ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

                return View(list);
            }
            catch (Exception)
            {
                TempData["CandidateError"] = "System error while loading candidate list.";
                ViewBag.Keyword = keyword;
                ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
                ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
                return View(new List<CandidateViewModel>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SendInterviewEmail(int id, DateTime? interviewDate, string interviewTime, string note)
        {
            if (!interviewDate.HasValue || string.IsNullOrWhiteSpace(interviewTime))
            {
                return Json(new { success = false, message = "Please select both interview date and time." });
            }
            interviewTime = interviewTime.Trim();

            DateTime timePart;
            if (!DateTime.TryParseExact(
                    interviewTime,
                    new[] { "HH:mm", "H:mm", "HH:mm:ss", "H:mm:ss" },
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out timePart))
            {
                return Json(new
                {
                    success = false,
                    message = "Interview time format is not valid. Please use HH:mm format (e.g. 14:30)."
                });
            }
            var interviewDateTime = interviewDate.Value.Date
                .AddHours(timePart.Hour)
                .AddMinutes(timePart.Minute)
                .AddSeconds(timePart.Second);
            if (interviewDateTime <= DateTime.Now)
            {
                return Json(new
                {
                    success = false,
                    message = "Interview time must be later than the current time."
                });
            }

            var candidate = db.HOSOVIECLAMs.FirstOrDefault(x => x.ID == id);
            if (candidate == null)
            {
                return Json(new { success = false, message = "Candidate not found." });
            }

            string fullName = candidate.TENUNGVIEN ?? "Candidate";
            string scheduleText = interviewDateTime.ToString("dd/MM/yyyy HH:mm");

            string from = "httbworkstation@gmail.com";
            string pass = "cotu wurg gbve crbk";

            string to = candidate.EMAIL;
            string subject = "[TBT Center] Interview invitation";

            string extraNote = string.IsNullOrWhiteSpace(note)
                ? ""
                : $"<p style='font-size:13px;line-height:1.6;color:#d0d0d0;'>{System.Net.WebUtility.HtmlEncode(note)}</p>";

            string body = $@"
        <html>
        <head>
        <meta charset='UTF-8' />
        <style>
        @media only screen and (max-width: 600px) {{
            .container {{ width: 94% !important; }}
            .section {{ padding: 20px 18px !important; }}
        }}
        </style>
        </head>

        <body style='font-family:Segoe UI, Arial, sans-serif;background:#f4f4f4;margin:0;padding:20px;'>

        <div class='container' style='max-width:600px;margin:auto;background:#111;
                    color:#f5f5f5;border-radius:12px;overflow:hidden;
                    box-shadow:0 10px 25px rgba(0,0,0,0.35);'>

            <div style='background:linear-gradient(135deg,#fceabb,#f8b500);padding:20px 26px;'>
                <h2 style='margin:0;color:#1a1a1a;'>TBT Center</h2>
                <p style='margin:4px 0 0;font-size:13px;color:#4a3b0a;'>Interview invitation</p>
            </div>

            <div class='section' style='padding:26px 32px;'>
                <p style='font-size:14px;line-height:1.6;margin-top:0;'>
                    Hello <b>{fullName}</b>,<br />
                    Thank you for applying to <b>TBT HR &amp; Finance Management System</b>.
                </p>

                <p style='font-size:13px;line-height:1.6;color:#d0d0d0;'>
                    We would like to invite you to an interview at the following time:
                </p>

                <div style='text-align:center;margin:14px 0 18px;'>
                    <div style='display:inline-block;padding:10px 20px;border-radius:999px;
                                background:linear-gradient(135deg,#fceabb,#f8b500);
                                color:#1a1a1a;font-size:16px;font-weight:600;'>
                        {scheduleText}
                    </div>
                </div>

                {extraNote}

                <p style='font-size:13px;line-height:1.6;color:#d0d0d0;'>
                    Please reply to this email if you need to reschedule or have any questions.
                </p>

                <p style='font-size:12px;color:#9c9c9c;margin-top:6px;'>
                    If you did not expect this email, you can ignore it.
                </p>
            </div>

            <div style='padding:14px 22px;border-top:1px solid #333;font-size:11px;color:#777;'>
                © {DateTime.Now.Year} TBT Center. All rights reserved.
            </div>
        </div>

        </body>
        </html>";

            try
            {
                using (var smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.EnableSsl = true;
                    smtp.Credentials = new NetworkCredential(from, pass);

                    var mail = new MailMessage();
                    mail.From = new MailAddress(from, "TBT Center");
                    mail.To.Add(to);
                    mail.Subject = subject;
                    mail.Body = body;
                    mail.IsBodyHtml = true;

                    smtp.Send(mail);
                }

                return Json(new { success = true, message = "Interview invitation has been sent." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Error while sending email. Please try again later." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DownloadCandidateFiles(int id)
        {
            try
            {
                var candidate = db.HOSOVIECLAMs.FirstOrDefault(x => x.ID == id);
                if (candidate == null)
                {
                    TempData["CandidateError"] = "Candidate not found.";
                    return RedirectToAction("Candidate");
                }

                // đúng với JobSeekerController: Content/Uploads/HoSoUngVien
                string root = Server.MapPath("~/Content/Uploads/HoSoUngVien/");
                var files = new List<Tuple<string, string>>();

                void AddFileIfExists(string fileName)
                {
                    if (string.IsNullOrWhiteSpace(fileName)) return;

                    string folderName = BuildFolderName(candidate.TENUNGVIEN, fileName);
                    string path = string.IsNullOrEmpty(folderName)
                        ? Path.Combine(root, fileName)
                        : Path.Combine(root, folderName, fileName);

                    if (System.IO.File.Exists(path))
                    {
                        files.Add(Tuple.Create(path, fileName));
                    }
                }

                AddFileIfExists(candidate.FILETHONGTIN);
                AddFileIfExists(candidate.FILEBANGCAP);
                AddFileIfExists(candidate.FILEKHAC);

                if (!files.Any())
                {
                    TempData["CandidateError"] = "No files were found for this candidate.";
                    return RedirectToAction("Candidate");
                }

                using (var ms = new MemoryStream())
                {
                    using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
                    {
                        foreach (var f in files)
                        {
                            var entry = archive.CreateEntry(f.Item2, CompressionLevel.Fastest);
                            using (var entryStream = entry.Open())
                            using (var fileStream = System.IO.File.OpenRead(f.Item1))
                            {
                                fileStream.CopyTo(entryStream);
                            }
                        }
                    }

                    ms.Position = 0;
                    string zipName = "Candidate_" + id + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".zip";
                    return File(ms.ToArray(), "application/zip", zipName);
                }
            }
            catch (Exception)
            {
                TempData["CandidateError"] = "System error while preparing candidate files.";
                return RedirectToAction("Candidate");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ExportAndDelete(string selectedIds)
        {
            if (string.IsNullOrWhiteSpace(selectedIds))
            {
                TempData["CandidateError"] = "Please select at least one candidate.";
                return RedirectToAction("Candidate");
            }

            var idList = new List<int>();
            foreach (var s in selectedIds.Split(','))
            {
                if (int.TryParse(s, out int id))
                    idList.Add(id);
            }

            if (!idList.Any())
            {
                TempData["CandidateError"] = "Selected candidate ID list is not valid.";
                return RedirectToAction("Candidate");
            }

            try
            {
                var candidates = db.HOSOVIECLAMs
                    .Where(x => idList.Contains(x.ID))
                    .OrderBy(x => x.ID)
                    .ToList();

                if (!candidates.Any())
                {
                    TempData["CandidateError"] = "No selected candidates were found.";
                    return RedirectToAction("Candidate");
                }

                byte[] bytes;
                using (var ms = new MemoryStream())
                {
                    var doc = new Document(PageSize.A4, 40, 40, 40, 40);
                    PdfWriter.GetInstance(doc, ms);
                    doc.Open();

                    iTextFont titleFont = FontFactory.GetFont("Helvetica", 16, iTextFont.BOLD);
                    iTextFont normalFont = FontFactory.GetFont("Helvetica", 11, iTextFont.NORMAL);

                    foreach (var c in candidates)
                    {
                        doc.Add(new Paragraph("Candidate #" + c.ID, titleFont));
                        doc.Add(new Paragraph("Full name: " + c.TENUNGVIEN, normalFont));
                        doc.Add(new Paragraph("Email: " + c.EMAIL, normalFont));
                        doc.Add(new Paragraph("File - Info: " + c.FILETHONGTIN, normalFont));
                        doc.Add(new Paragraph("File - Degree: " + c.FILEBANGCAP, normalFont));
                        doc.Add(new Paragraph("File - Others: " + c.FILEKHAC, normalFont));
                        doc.Add(new Paragraph("Generated at: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"), normalFont));

                        doc.Add(new Paragraph(" ", normalFont));
                        doc.Add(new LineSeparator());
                        doc.Add(new Paragraph(" ", normalFont));
                    }

                    doc.Close();
                    bytes = ms.ToArray();
                }

                db.HOSOVIECLAMs.RemoveRange(candidates);
                db.SaveChanges();

                string fileName = "Candidates_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".pdf";
                return File(bytes, "application/pdf", fileName);
            }
            catch (Exception)
            {
                TempData["CandidateError"] = "System error while exporting / deleting candidates.";
                return RedirectToAction("Candidate");
            }
        }

        private static DateTime? ExtractDateFromFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return null;

            var baseName = Path.GetFileNameWithoutExtension(fileName);
            if (string.IsNullOrEmpty(baseName)) return null;

            var m = Regex.Match(baseName, @"\d{14}|\d{12}|\d{10}");
            if (!m.Success) return null;

            string timeStr = m.Value;
            string[] formats;
            if (timeStr.Length == 14)
                formats = new[] { "yyyyMMddHHmmss" };
            else if (timeStr.Length == 12)
                formats = new[] { "yyyyMMddHHmm" };
            else
                formats = new[] { "yyyyMMddHH" };

            if (DateTime.TryParseExact(timeStr, formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime dt))
            {
                return dt;
            }

            return null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
