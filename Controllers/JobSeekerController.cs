using System;
using System.IO;
using System.Web;
using System.Web.Mvc;
using QLNSVATC.Models;
using QLNSVATC.Helpers;
using System.Linq;

namespace QLNSVATC.Controllers
{
    public class JobSeekerController : Controller
    {
        private QLNSVATCEntities db = new QLNSVATCEntities();

        bool IsValid(string fileName, string[] exts)
        {
            string ext = Path.GetExtension(fileName)?.ToLower();
            return exts.Contains(ext);
        }

        // GET: JobSeeker
        [HttpGet]
        public ActionResult SubmitCV()
        {
            var st = SettingsHelper.BuildViewBagData(db, null); // null => dùng GUEST
            ViewBag.Settings = st;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitCV(
    HOSOVIECLAM model,
    HttpPostedFileBase fileThongTin,
    HttpPostedFileBase fileBangCap,
    HttpPostedFileBase fileKhac)
        {
            try
            {
                // Validate Name
                if (string.IsNullOrWhiteSpace(model.TENUNGVIEN))
                {
                    ModelState.AddModelError("TENUNGVIEN", "Please fill in your name.");
                    var st1 = SettingsHelper.BuildViewBagData(db, null);
                    ViewBag.Settings = st1;
                    return View(model);
                }

                if (model.TENUNGVIEN.Length > 100)
                {
                    ModelState.AddModelError("TENUNGVIEN", "Do not exceed 100 characters.");
                    var st2 = SettingsHelper.BuildViewBagData(db, null);
                    ViewBag.Settings = st2;
                    return View(model);
                }

                // Validate Info File
                if (fileThongTin == null || fileThongTin.ContentLength == 0)
                {
                    ModelState.AddModelError("fileThongTin", "Please upload your personal information file.");
                    var st3 = SettingsHelper.BuildViewBagData(db, null);
                    ViewBag.Settings = st3;
                    return View(model);
                }

                if (!IsValid(fileThongTin.FileName, new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" }))
                {
                    ModelState.AddModelError("fileThongTin", "Invalid file type!");
                    var st4 = SettingsHelper.BuildViewBagData(db, null);
                    ViewBag.Settings = st4;
                    return View(model);
                }

                // Validate Degree File
                if (fileBangCap == null || fileBangCap.ContentLength == 0)
                {
                    ModelState.AddModelError("fileBangCap", "Please upload your degree file.");
                    var stc = SettingsHelper.BuildViewBagData(db, null);
                    ViewBag.Settings = stc;
                    return View(model);
                }

                if (!IsValid(fileBangCap.FileName, new[] { ".pdf", ".jpg", ".jpeg", ".png" }))
                {
                    ModelState.AddModelError("fileBangCap", "Invalid file type!");
                    var st5 = SettingsHelper.BuildViewBagData(db, null);
                    ViewBag.Settings = st5;
                    return View(model);
                }
                // Validate Other File
                if (fileKhac != null && fileKhac.ContentLength > 0)
                {
                    if (!IsValid(fileKhac.FileName, new[] { ".pdf", ".doc", ".docx", ".zip" }))
                    {
                        ModelState.AddModelError("fileKhac", "Invalid file type!");
                        var st6 = SettingsHelper.BuildViewBagData(db, null);
                        ViewBag.Settings = st6;
                        return View(model);
                    }
                }

                // === CHỈ LÚC NÀY MỚI LÀM VIỆC VỚI FILE ===
                string rootFolder = Server.MapPath("~/Uploads/HoSoUngVien/");
                if (!Directory.Exists(rootFolder))
                    Directory.CreateDirectory(rootFolder);

                DateTime now = DateTime.Now;
                string folderName = FileHelper.BuildCandidateFolderName(model.TENUNGVIEN, now);
                string fullFolderPath = Path.Combine(rootFolder, folderName);

                if (!Directory.Exists(fullFolderPath))
                    Directory.CreateDirectory(fullFolderPath);

                // Lưu file thông tin
                if (fileThongTin != null && fileThongTin.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(fileThongTin.FileName);
                    string savePath = Path.Combine(fullFolderPath, fileName);
                    fileThongTin.SaveAs(savePath);
                    model.FILETHONGTIN = fileName;
                }

                // Lưu file bằng cấp
                if (fileBangCap != null && fileBangCap.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(fileBangCap.FileName);
                    string savePath = Path.Combine(fullFolderPath, fileName);
                    fileBangCap.SaveAs(savePath);
                    model.FILEBANGCAP = fileName;
                }

                // Lưu file khác
                if (fileKhac != null && fileKhac.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(fileKhac.FileName);
                    string savePath = Path.Combine(fullFolderPath, fileName);
                    fileKhac.SaveAs(savePath);
                    model.FILEKHAC = fileName;
                }

                // Lưu vào DB
                db.HOSOVIECLAMs.Add(model);
                db.SaveChanges();

                // Set success message và redirect
                TempData["SuccessMsg"] = "File sent, we'll contact you later!";
                return RedirectToAction("SubmitCV");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error: " + ex.Message);
                var st = SettingsHelper.BuildViewBagData(db, null);
                ViewBag.Settings = st;
                return View(model);
            }

        }
    }
}