using System;
using System.IO;
using System.Web;
using System.Web.Mvc;
using QLNSVATC.Models;
using QLNSVATC.Helpers;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text.RegularExpressions;

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
            var st = SettingsHelper.BuildViewBagData(db, null);
            ViewBag.Settings = st;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitCV(HOSOVIECLAM model, HttpPostedFileBase fileThongTin, HttpPostedFileBase fileBangCap, HttpPostedFileBase fileKhac)
        {
            try
            {
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

                if (string.IsNullOrWhiteSpace(model.EMAIL))
                {
                    ModelState.AddModelError("EMAIL", "Please fill in your email.");
                    var stEmail = SettingsHelper.BuildViewBagData(db, null);
                    ViewBag.Settings = stEmail;
                    return View(model);
                }

                if (model.EMAIL.Length > 150 ||
                    !Regex.IsMatch(model.EMAIL, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    ModelState.AddModelError("EMAIL", "Invalid email format.");
                    var stEmail2 = SettingsHelper.BuildViewBagData(db, null);
                    ViewBag.Settings = stEmail2;
                    return View(model);
                }

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

                string rootFolder = Server.MapPath("~/Uploads/HoSoUngVien/");
                if (!Directory.Exists(rootFolder))
                    Directory.CreateDirectory(rootFolder);

                DateTime now = DateTime.Now;
                string folderName = FileHelper.BuildCandidateFolderName(model.TENUNGVIEN, now);
                string fullFolderPath = Path.Combine(rootFolder, folderName);

                if (!Directory.Exists(fullFolderPath))
                    Directory.CreateDirectory(fullFolderPath);

                if (fileThongTin != null && fileThongTin.ContentLength > 0)
                {
                    string ext = Path.GetExtension(fileThongTin.FileName);
                    string newFileName = FileHelper.BuildNormalizedFileName(
                        model.TENUNGVIEN,
                        "fileThongTin",
                        now,
                        ext
                    );

                    string savePath = Path.Combine(fullFolderPath, newFileName);
                    fileThongTin.SaveAs(savePath);
                    model.FILETHONGTIN = newFileName;
                }

                if (fileBangCap != null && fileBangCap.ContentLength > 0)
                {
                    string ext = Path.GetExtension(fileBangCap.FileName);
                    string newFileName = FileHelper.BuildNormalizedFileName(
                        model.TENUNGVIEN,
                        "fileBangCap",
                        now,
                        ext
                    );

                    string savePath = Path.Combine(fullFolderPath, newFileName);
                    fileBangCap.SaveAs(savePath);
                    model.FILEBANGCAP = newFileName;
                }

                if (fileKhac != null && fileKhac.ContentLength > 0)
                {
                    string ext = Path.GetExtension(fileKhac.FileName);
                    string newFileName = FileHelper.BuildNormalizedFileName(
                        model.TENUNGVIEN,
                        "fileKhac",
                        now,
                        ext
                    );

                    string savePath = Path.Combine(fullFolderPath, newFileName);
                    fileKhac.SaveAs(savePath);
                    model.FILEKHAC = newFileName;
                }

                db.HOSOVIECLAMs.Add(model);
                db.SaveChanges();

                try
                {
                    var from = "httbworkstation@gmail.com";
                    var pass = "cotu wurg gbve crbk";  

                    var fromAddress = new MailAddress(from, "TBT Center HR");
                    var toAddress = new MailAddress(model.EMAIL, model.TENUNGVIEN);

                    string subject = "TBT Center HR - Application received";

                    string body = $@"
                    <html>
                    <head>
                        <meta charset='UTF-8' />
                        <style>
                            @media only screen and (max-width: 600px) {{
                                .container {{ width: 94% !important; }}
                                .section {{ padding: 20px 18px !important; }}
                                .files-box {{ padding: 14px 16px !important; }}
                            }}
                        </style>
                    </head>

                    <body style='font-family:Segoe UI, Arial, sans-serif;background:#f4f4f4;margin:0;padding:20px;'>

                        <div class='container' style='max-width:600px;margin:auto;background:#111827;
                                    color:#f5f5f5;border-radius:12px;overflow:hidden;
                                    box-shadow:0 10px 25px rgba(0,0,0,0.35);'>

                            <!-- HEADER -->
                            <div style='background:linear-gradient(135deg,#fceabb,#F9C12A,#f8b500);padding:20px 26px;'>
                                <h2 style='margin:0;color:#1f2933;'>TBT Center</h2>
                                <p style='margin:4px 0 0;font-size:13px;color:#4b5563;'>Application confirmation</p>
                            </div>

                            <!-- BODY -->
                            <div class='section' style='padding:26px 32px;'>

                                <p style='font-size:14px;line-height:1.6;margin-top:0;'>
                                    Dear <b>{model.TENUNGVIEN}</b>,<br />
                                    We have received your application. Below is a quick summary:
                                </p>

                                <div class='info-box' style='margin:18px 0 16px;padding:12px 16px;border-radius:12px;
                                        background:#020617;border:1px solid rgba(148,163,184,0.55);'>
                                    <p style='margin:0;font-size:14px;'><strong>Name:</strong> {model.TENUNGVIEN}</p>
                                    <p style='margin:4px 0;font-size:14px;'><strong>Email:</strong> {model.EMAIL}</p>
                                    <p style='margin:4px 0;font-size:14px;'><strong>Time:</strong> {now:dd/MM/yyyy HH:mm}</p>
                                </div>

                                <p style='margin:0 0 8px;font-size:13px;color:#e5e7eb;font-weight:600;'>
                                    Files sent to us:
                                </p>

                                <div class='files-box' style='margin-bottom:18px;padding:10px 14px;border-radius:10px;
                                        background:#020617;border:1px dashed rgba(249,193,42,0.6);'>
                                    <ul style='margin:0;padding-left:18px;font-size:13px;'>
                                        {(string.IsNullOrEmpty(model.FILETHONGTIN) ? "" : $"<li>Personal Information: {model.FILETHONGTIN}</li>")}
                                        {(string.IsNullOrEmpty(model.FILEBANGCAP) ? "" : $"<li>Degree: {model.FILEBANGCAP}</li>")}
                                        {(string.IsNullOrEmpty(model.FILEKHAC) ? "" : $"<li>Others: {model.FILEKHAC}</li>")}
                                    </ul>
                                    <p style='margin:10px 0 0;font-size:12px;color:#9ca3af;'>
                                        These files are also attached to this email for your reference.
                                    </p>
                                </div>

                                <p style='font-size:13px;line-height:1.6;margin:0;color:#d1d5db;'>
                                    Our team will review your application and contact you as soon as possible.<br />
                                    Best regards,<br />
                                    <span style='color:#F9C12A;font-weight:600;'>TBT Center HR</span>
                                </p>

                            </div>

                            <!-- FOOTER -->
                            <div style='padding:14px 22px;border-top:1px solid #1f2937;font-size:11px;color:#6b7280;
                                        background:#020617;'>
                                © {DateTime.Now.Year} TBT Center. All rights reserved.
                            </div>
                        </div>

                    </body>
                    </html>";


                    using (var smtp = new SmtpClient("smtp.gmail.com", 587))
                    {
                        smtp.EnableSsl = true;
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials = new NetworkCredential(from, pass);

                        using (var message = new MailMessage(fromAddress, toAddress))
                        {
                            message.Subject = subject;
                            message.Body = body;
                            message.IsBodyHtml = true;

                            if (!string.IsNullOrEmpty(model.FILETHONGTIN))
                            {
                                string path = Path.Combine(fullFolderPath, model.FILETHONGTIN);
                                if (System.IO.File.Exists(path))
                                    message.Attachments.Add(new Attachment(path));
                            }
                            if (!string.IsNullOrEmpty(model.FILEBANGCAP))
                            {
                                string path = Path.Combine(fullFolderPath, model.FILEBANGCAP);
                                if (System.IO.File.Exists(path))
                                    message.Attachments.Add(new Attachment(path));
                            }
                            if (!string.IsNullOrEmpty(model.FILEKHAC))
                            {
                                string path = Path.Combine(fullFolderPath, model.FILEKHAC);
                                if (System.IO.File.Exists(path))
                                    message.Attachments.Add(new Attachment(path));
                            }

                            smtp.Send(message);
                        }
                    }
                }
                catch
                {
                }

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