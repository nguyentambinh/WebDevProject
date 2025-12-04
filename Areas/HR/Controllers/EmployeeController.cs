using System;
using System.Data.Entity.Infrastructure;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using QLNSVATC.Helpers;
using QLNSVATC.Models;

namespace QLNSVATC.Areas.HR.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly QLNSVATCEntities db = new QLNSVATCEntities();

        public ActionResult Information(string keyword, string maPB, string maCV, string loaiNV)
        {
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return Json(new { success = false, message = "Invalid employee id." });

            var nv = db.NHANVIENs.Find(id);
            if (nv == null)
                return Json(new { success = false, message = "Employee not found." });

            try
            {
                db.NHANVIENs.Remove(nv);
                db.SaveChanges();

                return Json(new { success = true, message = "Employee has been deleted successfully." });
            }
            catch (Exception)
            {
                return Json(new
                {
                    success = false,
                    message = "Cannot delete this employee because there are related records (attendance, salary, contracts, etc.). Please remove or update related data first."
                });
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

            nv.MAPB = maPB;
            nv.MACV = maCV;
            db.SaveChanges();

            return Json(new { success = true });
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

                var employeesList = db.NHANVIENs
                    .Include("PHONGBAN")
                    .Include("VITRICONGVIEC")
                    .Include("LUONG")
                    .OrderBy(nv => nv.MANV)
                    .ToList();

                ViewBag.OpenCreateModal = true;
                ViewBag.CreateModel = model;
                TempData["CreateError"] = "Some information is missing or invalid. Please check the form and try again.";

                return View("Information", employeesList);
            }

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

        [HttpGet]
        [ActionName("Profile")]
        public ActionResult ProfileGet(string id)
        {
            var userId = Session["UserId"] as string;
            var st = SettingsHelper.BuildViewBagData(db, userId);
            ViewBag.Settings = st;

            // provide lists for dropdowns in Profile view
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
