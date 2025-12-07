using System;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Web.Mvc;
using QLNSVATC.Models;
using QLNSVATC.Helpers;   

namespace QLNSVATC.Controllers
{
    public class AccountController : Controller
    {
        private QLNSVATCEntities db = new QLNSVATCEntities();

        //LOGIN

        [HttpGet]
        public ActionResult Login(string username)
        {
            USER u = new USER { USERNAME = username };
            return View(u);
        }

        [HttpPost]
        public ActionResult Login(string username, string pass)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(pass))
            {
                ModelState.AddModelError("", "Please enter all required information.");
                return View(new USER { USERNAME = username });
            }

            var user = db.USERS.FirstOrDefault(x => x.USERNAME == username);

            if (user == null)
            {
                ModelState.AddModelError("", "Account does not exist.");
                return View(new USER { USERNAME = username });
            }

            string hash = pass.HashPassword();
            if (user.PASS != hash)
            {
                ModelState.AddModelError("", "Incorrect password.");
                return View(new USER { USERNAME = username });
            }

            string auth = user.AUTH;
            var authInfo = db.CONFIRMAUTHs.FirstOrDefault(x => x.AUTH == auth);

            if (authInfo == null)
            {
                ModelState.AddModelError("", "Permission data not found.");
                return View(new USER { USERNAME = username });
            }

            string codeBus = authInfo.CODEBUS;
            string prefix = auth.Substring(0, 2).ToUpper();  
            string code = authInfo.CODE;                      

            var nv = db.NHANVIENs.FirstOrDefault(x => x.MANV == code);

            if (nv != null)
            {
                Session["UserId"] = nv.MANV;
                Session["FullName"] = nv.TENNV;
                Session["Role"] = prefix;

                Session["AuthCode"] = auth;

                Session["BusinessCode"] = codeBus;

                var st = db.USER_SETTINGS.FirstOrDefault(x => x.UserId == nv.MANV);

                if (st != null)
                {
                    Session["ThemeCode"] = st.ThemeCode;
                    Session["DarkMode"] = st.DarkMode;
                    Session["LanguageCode"] = st.LanguageCode;
                    Session["FontCode"] = st.FontCode;
                    Session["FontSize"] = st.FontSize;
                    Session["LayoutCode"] = st.LayoutCode;
                }

                LogHelper.WriteLog(
                    db,
                    "Login",
                    username,
                    $"Nhân viên có mã {nv.MANV} đã đăng nhập hệ thống."
                );
            }
            switch (prefix)
            {
                case "AD": return RedirectToAction("Index", "Home", new { area = "Admin" });
                case "HR": return RedirectToAction("Index", "Home", new { area = "HR", id = codeBus });
                case "FN": return RedirectToAction("Index", "Home", new { area = "FN", id = codeBus });
                case "OF": return RedirectToAction("Index", "Home", new { area = "", id = codeBus });
                case "EM": return RedirectToAction("Index", "Home", new { area = "", id = codeBus });
            }

            ModelState.AddModelError("", "Invalid role.");
            return View(new USER { USERNAME = username });
        }


        //REGISTER

        [HttpGet]
        public ActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        public ActionResult Register(RegisterViewModel model)
        {
            ViewBag.ShowOtp = false;

            if (string.IsNullOrEmpty(model.EmployeeCode) ||
                string.IsNullOrEmpty(model.Email) ||
                string.IsNullOrEmpty(model.USERNAME) ||
                string.IsNullOrEmpty(model.PASS) ||
                string.IsNullOrEmpty(model.ConfirmPassword))
            {
                ModelState.AddModelError("", "Please fill in all fields.");
                return View(model);
            }

            if (!model.PASS.IsStrongPassword())
            {
                ModelState.AddModelError("", "Strong Password - EX: Abc@1234");
                return View(model);
            }

            if (model.PASS != model.ConfirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match.");
                return View(model);
            }

            var nv = db.NHANVIENs.FirstOrDefault(x => x.MANV == model.EmployeeCode);
            if (nv == null)
            {
                ModelState.AddModelError("", "Employee does not exist.");
                return View(model);
            }

            var confirm = db.CONFIRMAUTHs.FirstOrDefault(x => x.CODE == model.EmployeeCode);
            if (confirm != null)
            {
                ModelState.AddModelError("", "This employee already has an account.");
                return View(model);
            }

            var existU = db.USERS.FirstOrDefault(x => x.USERNAME == model.USERNAME);
            if (existU != null)
            {
                ModelState.AddModelError("", "Username already exists.");
                return View(model);
            }

            Random r = new Random();
            string otp = r.Next(100000, 999999).ToString();

            Session["OTP"] = otp;
            Session["RegData"] = model;

            string fullName = nv.TENNV ?? "User";
            SendOtpRegister(model.Email, otp, fullName);

            ViewBag.ShowOtp = true;
            return View(model);
        }

        //CONFIRM OTP REGISTER

        [HttpPost]
        public ActionResult ConfirmOtp(RegisterViewModel model)
        {
            string otp = Session["OTP"]?.ToString();
            var data = Session["RegData"] as RegisterViewModel;

            if (otp == null || data == null)
            {
                ModelState.AddModelError("", "Registration session has expired.");
                return View("Register", model);
            }

            if (model.OtpCode != otp)
            {
                ModelState.AddModelError("", "Incorrect OTP.");
                ViewBag.ShowOtp = true;
                return View("Register", model);
            }

            var nv = db.NHANVIENs.FirstOrDefault(x => x.MANV == data.EmployeeCode);
            var vt = db.VITRICONGVIECs.FirstOrDefault(x => x.MACV == nv.MACV);

            string auth;

            if (nv.MACV == "VPQL36") auth = "OF";
            else if (nv.MACV == "QLNS36") auth = "HR";
            else if (nv.MACV == "QLTC36") auth = "FN";
            else
            {
                string lastTwo = nv.MADN.Length >= 2
                    ? nv.MADN.Substring(nv.MADN.Length - 2, 2)
                    : nv.MADN;

                string random3 = new Random().Next(100, 999).ToString();

                auth = "EM" + lastTwo + random3;
            }

            USER u = new USER
            {
                USERNAME = data.USERNAME,
                PASS = data.PASS.HashPassword(),
                AUTH = auth
            };
            db.USERS.Add(u);

            db.CONFIRMAUTHs.Add(new CONFIRMAUTH
            {
                AUTH = auth,
                NAMEAUTH = vt != null ? vt.TENCV : "Employee",
                CODE = nv.MANV,
                CODEBUS = nv.MADN
            });

            var tt = db.THONGTINLIENHEs.FirstOrDefault(x => x.MANV == nv.MANV);
            if (tt != null)
                tt.GMAIL = data.Email;
            else
            {
                db.THONGTINLIENHEs.Add(new THONGTINLIENHE
                {
                    MANV = nv.MANV,
                    DIACHI = "Updating...",
                    SODT = "0000000000",
                    FB = "",
                    GMAIL = data.Email,
                    QUEQUAN = 0
                });
            }

            db.SaveChanges();
            LogHelper.WriteLog(
                db,
                "CreateAccount",
                data.USERNAME,
                $"Nhân viên có mã {nv.MANV} đã tạo tài khoản mới."
            );

            Session.Remove("OTP");
            Session.Remove("RegData");

            return RedirectToAction("Login",
                new { username = data.USERNAME });
        }

        //SEND EMAIL OTP REGISTER

        private void SendOtpRegister(string email, string otp, string fullName)
        {
            var from = "httbworkstation@gmail.com";
            var pass = "cotu wurg gbve crbk";

            var subject = "TBT Center - OTP Confirmation";

            var body = $@"
                        <html>
                        <head>
                        <meta charset='UTF-8' />
                        <style>
                        @media only screen and (max-width: 600px) {{
                            .container {{ width: 94% !important; }}
                            .section {{ padding: 20px 18px !important; }}
                            .otp-box {{ font-size: 20px !important; padding: 10px 18px !important; letter-spacing: 4px !important; }}
                        }}
                        </style>
                        </head>

                        <body style='font-family:Segoe UI, Arial, sans-serif;background:#f4f4f4;margin:0;padding:20px;'>

                        <div class='container' style='max-width:600px;margin:auto;background:#111;
                                    color:#f5f5f5;border-radius:12px;overflow:hidden;
                                    box-shadow:0 10px 25px rgba(0,0,0,0.35);'>

                            <div style='background:linear-gradient(135deg,#fceabb,#f8b500);padding:20px 26px;'>
                                <h2 style='margin:0;color:#1a1a1a;'>TBT Center</h2>
                                <p style='margin:4px 0 0;font-size:13px;color:#4a3b0a;'>Account verification code</p>
                            </div>

                            <div class='section' style='padding:26px 32px;'>
                                <p style='font-size:14px;line-height:1.6;margin-top:0;'>
                                    Hello <b>{fullName}</b>,<br />
                                    Thank you for registering on <b>TBT HR & Finance Management System</b>.
                                </p>

                                <p style='margin:18px 0 8px;font-size:13px;color:#bbbbbb;'>Your OTP:</p>

                                <div style='text-align:center;margin:12px 0 20px;'>
                                    <span class='otp-box' style='display:inline-block;padding:12px 22px;
                                        border-radius:999px;background:linear-gradient(135deg,#fceabb,#f8b500);
                                        color:#1a1a1a;font-size:22px;font-weight:700;letter-spacing:6px;'>
                                        {otp}
                                    </span>
                                </div>

                                <p style='font-size:13px;line-height:1.6;color:#d0d0d0;'>
                                    Enter this code on the registration screen to complete your account creation.
                                </p>

                                <p style='font-size:12px;color:#9c9c9c;margin-top:6px;'>
                                    If you did not perform this action, just ignore this email.
                                </p>
                            </div>

                            <div style='padding:14px 22px;border-top:1px solid #333;font-size:11px;color:#777;'>
                                © {DateTime.Now.Year} TBT Center. All rights reserved.
                            </div>
                        </div>

                        </body>
                        </html>";


            MailMessage mail = new MailMessage();
            mail.From = new MailAddress(from, "TBT Center");
            mail.To.Add(email);
            mail.Subject = subject;
            mail.Body = body;
            mail.IsBodyHtml = true;

            new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(from, pass)
            }.Send(mail);
        }

        //FORGOT PASSWORD

        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        public ActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Email))
            {
                ModelState.AddModelError("", "Please fill in all fields.");
                return View(model);
            }

            var user = db.USERS.FirstOrDefault(x => x.USERNAME == model.Username);
            if (user == null)
            {
                ModelState.AddModelError("", "Account does not exist.");
                return View(model);
            }

            var cf = db.CONFIRMAUTHs.FirstOrDefault(x => x.AUTH == user.AUTH);
            var nv = db.NHANVIENs.FirstOrDefault(x => x.MANV == cf.CODE);
            var tt = db.THONGTINLIENHEs.FirstOrDefault(x => x.MANV == nv.MANV);

            if (tt == null || tt.GMAIL != model.Email)
            {
                ModelState.AddModelError("", "Email does not match.");
                return View(model);
            }

            string otp = new Random().Next(100000, 999999).ToString();

            Session["FP_OTP"] = otp;
            Session["FP_Username"] = model.Username;

            SendOtpForgot(model.Email, otp, nv.TENNV);

            model.ShowOtp = true;
            return View(model);
        }

        //SEND EMAIL OTP FORGOT

        private void SendOtpForgot(string email, string otp, string fullName)
        {
            var from = "httbworkstation@gmail.com";
            var pass = "cotu wurg gbve crbk";

            var subject = "TBT Center - Password Reset OTP";
            var body = $@"
                    <html>
                    <head>
                    <meta charset='UTF-8' />
                    <style>
                    @media only screen and (max-width: 600px) {{
                        .container {{ width: 94% !important; }}
                        .section {{ padding: 20px 18px !important; }}
                        .otp-box {{ padding: 10px 18px !important;font-size:20px !important;letter-spacing:4px !important; }}
                    }}
                    </style>
                    </head>

                    <body style='font-family:Segoe UI, Arial,sans-serif;background:#f4f4f4;margin:0;padding:20px;'>

                    <div class='container' style='max-width:600px;margin:auto;background:#111;
                                color:#f5f5f5;border-radius:12px;overflow:hidden;
                                box-shadow:0 10px 25px rgba(0,0,0,0.35);'>

                        <div style='background:linear-gradient(135deg,#fceabb,#f8b500);padding:20px 26px;'>
                            <h2 style='margin:0;color:#1a1a1a;'>TBT Center</h2>
                            <p style='margin:4px 0 0;font-size:13px;color:#4a3b0a;'>Password reset verification</p>
                        </div>

                        <div class='section' style='padding:26px 32px;'>

                            <p style='font-size:14px;line-height:1.6;margin-top:0;'>
                                Hello <b>{fullName}</b>,<br />
                                You requested to reset your TBT account password.
                            </p>

                            <p style='margin:18px 0 8px;font-size:13px;color:#bbbbbb;'>Your OTP:</p>

                            <div style='text-align:center;margin:12px 0 20px;'>
                                <span class='otp-box' style='display:inline-block;padding:12px 22px;
                                    border-radius:999px;background:linear-gradient(135deg,#fceabb,#f8b500);
                                    color:#1a1a1a;font-size:22px;font-weight:700;letter-spacing:6px;'>
                                    {otp}
                                </span>
                            </div>

                            <p style='font-size:13px;line-height:1.6;color:#d0d0d0;'>
                                Enter this OTP in the reset screen to continue.
                            </p>

                            <p style='font-size:12px;color:#9c9c9c;margin-top:6px;'>
                                If you did not request this, ignore the email.
                            </p>
                        </div>

                        <div style='padding:14px 22px;border-top:1px solid #333;font-size:11px;color:#777;'>
                            © {DateTime.Now.Year} TBT Center. All rights reserved.
                        </div>
                    </div>

                    </body>
                    </html>";


            MailMessage mail = new MailMessage();
            mail.From = new MailAddress(from, "TBT Center");
            mail.To.Add(email);
            mail.Subject = subject;
            mail.Body = body;
            mail.IsBodyHtml = true;

            new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(from, pass)
            }.Send(mail);
        }

        //OTP CHECK

        [HttpPost]
        public ActionResult ForgotPasswordOtp(ForgotPasswordViewModel model)
        {
            string otp = Session["FP_OTP"]?.ToString();
            string username = Session["FP_Username"]?.ToString();

            if (otp == null || username == null)
            {
                ModelState.AddModelError("", "Session expired.");
                return View("ForgotPassword", model);
            }

            if (model.OtpCode != otp)
            {
                ModelState.AddModelError("", "Incorrect OTP.");
                model.ShowOtp = true;
                return View("ForgotPassword", model);
            }

            model.Username = username;
            model.ShowReset = true;
            return View("ForgotPassword", model);
        }

        //RESET PASSWORD

        [HttpPost]
        public ActionResult ResetPassword(ForgotPasswordViewModel model)
        {
            string username = Session["FP_Username"]?.ToString();

            if (username == null)
            {
                ModelState.AddModelError("", "Session expired.");
                return View("ForgotPassword", model);
            }

            if (!model.NewPass.IsStrongPassword())
            {
                ModelState.AddModelError("", "Strong Password - EX: Abc@1234");
                model.ShowReset = true;
                return View("ForgotPassword", model);
            }

            if (model.NewPass != model.ConfirmPass)
            {
                ModelState.AddModelError("", "Passwords do not match.");
                model.ShowReset = true;
                return View("ForgotPassword", model);
            }

            var user = db.USERS.FirstOrDefault(x => x.USERNAME == username);
            user.PASS = model.NewPass.HashPassword();
            db.SaveChanges();

            var cf = db.CONFIRMAUTHs.FirstOrDefault(x => x.AUTH == user.AUTH);
            string manv = cf != null ? cf.CODE : "UNKNOWN";

            LogHelper.WriteLog(
                db,
                "ChangePassword",
                username,
                $"Nhân viên có mã {manv} đã đổi mật khẩu thành công."
            );

            Session.Remove("FP_OTP");
            Session.Remove("FP_Username");

            model.Success = true;
            model.Username = username;

            return View("ForgotPassword", model);
        }

        //LOGOUT 

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }
        [HttpGet]
        [ActionName("Profile")]
        public ActionResult ProfileGet(string id)
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
    }
}
