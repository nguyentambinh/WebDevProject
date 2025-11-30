using System;
using System.IO;
using System.Web;
using System.Web.Mvc;
using QLNSVATC.Models;
using QLNSVATC.Helpers;

namespace QLNSVATC.Controllers
{
    public class JobSeekerController : Controller
    {
        private readonly QLNSVATCEntities db = new QLNSVATCEntities();
        // GET: JobSeeker
        public ActionResult SubmitCV()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(
            HOSOVIECLAM model,
            HttpPostedFileBase fileThongTin,
            HttpPostedFileBase fileBangCap,
            HttpPostedFileBase fileKhac)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.TENUNGVIEN))
                {
                    ModelState.AddModelError("", "Ứng viên vui lòng nhập tên.");
                    return View(model);
                }

                // Folder gốc lưu hồ sơ
                string rootFolder = Server.MapPath("~/Uploads/HoSoUngVien/");
                if (!Directory.Exists(rootFolder))
                    Directory.CreateDirectory(rootFolder);

                DateTime now = DateTime.Now;
                string folderName = FileHelper.BuildCandidateFolderName(model.TENUNGVIEN, now);
                string fullFolderPath = Path.Combine(rootFolder, folderName);

                if (!Directory.Exists(fullFolderPath))
                    Directory.CreateDirectory(fullFolderPath);

                // Lưu file THÔNG TIN CÁ NHÂN
                if (fileThongTin != null && fileThongTin.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(fileThongTin.FileName);
                    string savePath = Path.Combine(fullFolderPath, fileName);
                    fileThongTin.SaveAs(savePath);
                    model.FILETHONGTIN = $"/Uploads/HoSoUngVien/{folderName}/{fileName}";
                }

                // Lưu file BẰNG CẤP
                if (fileBangCap != null && fileBangCap.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(fileBangCap.FileName);
                    string savePath = Path.Combine(fullFolderPath, fileName);
                    fileBangCap.SaveAs(savePath);
                    model.FILEBANGCAP = $"/Uploads/HoSoUngVien/{folderName}/{fileName}";
                }

                // Lưu file KHÁC (CV, Portfolio…)
                if (fileKhac != null && fileKhac.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(fileKhac.FileName);
                    string savePath = Path.Combine(fullFolderPath, fileName);
                    fileKhac.SaveAs(savePath);

                    // CHANGED: đường dẫn tương đối để lưu DB
                    model.FILEKHAC = $"/Uploads/HoSoUngVien/{folderName}/{fileName}";
                }

                // Lưu record hồ sơ vào DB
                db.HOSOVIECLAMs.Add(model);
                db.SaveChanges();

                TempData["msg"] = "Gửi hồ sơ thành công!";
                return RedirectToAction("Create");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi: " + ex.Message);
                return View(model);
            }
        }
    }
}