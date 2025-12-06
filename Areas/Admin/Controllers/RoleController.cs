using System;
using System.Linq;
using System.Web.Mvc;
using QLNSVATC.Models;
using QLNSVATC.Helper; 
using QLNSVATC.Helpers; 

namespace QLNSVATC.Areas.Admin.Data
{
    public class RoleUserViewModel
    {
        public string UserName { get; set; }
        public string Auth { get; set; }
        public string Prefix { get; set; }
        public string Suffix { get; set; }

        public string NameAuth { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public string PositionName { get; set; }

        public string BusinessCode { get; set; }
        public string BusinessName { get; set; }
    }

    public class UnassignedEmployeeViewModel
    {
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public string PositionName { get; set; }
        public string BusinessCode { get; set; }
        public string BusinessName { get; set; }
    }
}

namespace QLNSVATC.Areas.Admin.Controllers
{
    using QLNSVATC.Areas.Admin.Data;

    public class RoleController : Controller
    {
        private readonly QLNSVATCEntities db = new QLNSVATCEntities();
        public ActionResult Index(string prefix)

        {
            if (!CheckAccess.Role("AD"))
            {
                Session.Clear();
                return RedirectToAction("Login", "Account", new { area = "" });
            }


            var query = from u in db.USERS
                        join c in db.CONFIRMAUTHs on u.AUTH equals c.AUTH
                        join nv in db.NHANVIENs on c.CODE equals nv.MANV into gnv
                        from nv in gnv.DefaultIfEmpty()
                        join cv in db.VITRICONGVIECs on nv.MACV equals cv.MACV into gcv
                        from cv in gcv.DefaultIfEmpty()
                        join dn in db.THONGTINDOANHNGHIEPs on c.CODEBUS equals dn.MADN into gdn
                        from dn in gdn.DefaultIfEmpty()
                        select new RoleUserViewModel
                        {
                            UserName = u.USERNAME,
                            Auth = u.AUTH,
                            Prefix = u.AUTH.Length >= 2 ? u.AUTH.Substring(0, 2) : u.AUTH,
                            Suffix = u.AUTH.Length > 2 ? u.AUTH.Substring(2) : "",
                            NameAuth = c.NAMEAUTH,
                            EmployeeCode = c.CODE,
                            EmployeeName = nv != null ? nv.HOLOT + " " + nv.TENNV : null,
                            PositionName = cv != null ? cv.TENCV : null,
                            BusinessCode = c.CODEBUS,
                            BusinessName = dn != null ? dn.TENDN : null
                        };

            if (!string.IsNullOrWhiteSpace(prefix))
            {
                query = query.Where(x => x.Prefix == prefix);
            }

            var model = query
                .OrderBy(x => x.Prefix)
                .ThenBy(x => x.UserName)
                .ToList();

            var usedCodes = db.CONFIRMAUTHs
                .Select(x => x.CODE)
                .Where(x => x != null)
                .ToList();

            var unassigned = (from nv in db.NHANVIENs
                              where !usedCodes.Contains(nv.MANV)
                              join cv in db.VITRICONGVIECs on nv.MACV equals cv.MACV into gcv
                              from cv in gcv.DefaultIfEmpty()
                              join dn in db.THONGTINDOANHNGHIEPs on nv.MADN equals dn.MADN into gdn
                              from dn in gdn.DefaultIfEmpty()
                              select new UnassignedEmployeeViewModel
                              {
                                  EmployeeCode = nv.MANV,
                                  EmployeeName = nv.HOLOT + " " + nv.TENNV,
                                  PositionName = cv != null ? cv.TENCV : null,
                                  BusinessCode = nv.MADN,
                                  BusinessName = dn != null ? dn.TENDN : null
                              })
                              .OrderBy(x => x.EmployeeCode)
                              .ToList();

            ViewBag.UnassignedEmployees = unassigned;
            ViewBag.CurrentPrefix = prefix ?? "";

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangeRole(string auth, string newPrefix)
        {
            if (!CheckAccess.Role("AD"))
            {
                Session.Clear();
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            if (string.IsNullOrWhiteSpace(auth) || string.IsNullOrWhiteSpace(newPrefix))
            {
                TempData["RoleError"] = "Invalid data.";
                return RedirectToAction("Index");
            }

            var user = db.USERS.FirstOrDefault(x => x.AUTH == auth);
            var cf = db.CONFIRMAUTHs.FirstOrDefault(x => x.AUTH == auth);

            if (user == null || cf == null)
            {
                TempData["RoleError"] = "Permission record not found.";
                return RedirectToAction("Index");
            }

            string suffix = auth.Length > 2 ? auth.Substring(2) : "";
            string newAuth = newPrefix + suffix;
            if (db.USERS.Any(x => x.AUTH == newAuth) || db.CONFIRMAUTHs.Any(x => x.AUTH == newAuth))
            {
                TempData["RoleError"] = "Target permission code already exists.";
                return RedirectToAction("Index", new { prefix = newPrefix });
            }

            string oldAuth = auth;
            string username = user.USERNAME;

            var newUser = new USER
            {
                USERNAME = user.USERNAME,
                PASS = user.PASS,
                AUTH = newAuth
            };
            db.USERS.Add(newUser);

            var newCf = new CONFIRMAUTH
            {
                AUTH = newAuth,
                NAMEAUTH = cf.NAMEAUTH,
                CODE = cf.CODE,
                CODEBUS = cf.CODEBUS
            };
            db.CONFIRMAUTHs.Add(newCf);

            db.USERS.Remove(user);
            db.CONFIRMAUTHs.Remove(cf);

            db.SaveChanges();

            LogHelper.WriteLog(
                db,
                "ChangeRole",
                username,
                $"Permission changed from {oldAuth} to {newAuth}."
            );

            TempData["RoleSuccess"] = "Role has been updated.";
            return RedirectToAction("Index", new { prefix = newPrefix });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddRole(string employeeCode, string rolePrefix, string roleName)
        {
            if (!CheckAccess.Role("AD"))
            {
                Session.Clear();
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            if (string.IsNullOrWhiteSpace(employeeCode) || string.IsNullOrWhiteSpace(rolePrefix))
            {
                TempData["RoleError"] = "Invalid data.";
                return RedirectToAction("Index");
            }

            var nv = db.NHANVIENs.FirstOrDefault(x => x.MANV == employeeCode);
            if (nv == null)
            {
                TempData["RoleError"] = "Employee not found.";
                return RedirectToAction("Index");
            }

            if (db.CONFIRMAUTHs.Any(x => x.CODE == employeeCode))
            {
                TempData["RoleError"] = "This employee already has a permission code.";
                return RedirectToAction("Index");
            }

            string defaultUserName = employeeCode.ToLower();

            if (db.USERS.Any(x => x.USERNAME == defaultUserName))
            {
                TempData["RoleError"] = "Username already exists.";
                return RedirectToAction("Index");
            }

            string lastTwo = (nv.MADN ?? "00");
            if (lastTwo.Length >= 2)
                lastTwo = lastTwo.Substring(lastTwo.Length - 2);
            else if (lastTwo.Length == 1)
                lastTwo = "0" + lastTwo;
            else
                lastTwo = "00";

            var rnd = new Random();
            string auth;
            do
            {
                var random3 = rnd.Next(100, 999).ToString();
                auth = rolePrefix + lastTwo + random3;
            }
            while (db.CONFIRMAUTHs.Any(x => x.AUTH == auth) || db.USERS.Any(x => x.AUTH == auth));

            string passPlain = "Abc@1234";
            string passHash = passPlain.HashPassword();

            var user = new USER
            {
                USERNAME = defaultUserName,
                PASS = passHash,
                AUTH = auth
            };
            db.USERS.Add(user);

            var cf = new CONFIRMAUTH
            {
                AUTH = auth,
                NAMEAUTH = string.IsNullOrWhiteSpace(roleName) ? "Employee" : roleName,
                CODE = nv.MANV,
                CODEBUS = nv.MADN
            };
            db.CONFIRMAUTHs.Add(cf);

            db.SaveChanges();

            LogHelper.WriteLog(
                db,
                "CreateAuth",
                user.USERNAME,
                $"New permission {auth} created for employee {nv.MANV}."
            );

            TempData["RoleSuccess"] = $"Permission has been created. Username: {defaultUserName}, default password: {passPlain}.";
            return RedirectToAction("Index", new { prefix = rolePrefix });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteRole(string auth)
        {
            if (!CheckAccess.Role("AD"))
            {
                Session.Clear();
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            if (string.IsNullOrWhiteSpace(auth))
            {
                TempData["RoleError"] = "Invalid permission code.";
                return RedirectToAction("Index");
            }

            var user = db.USERS.FirstOrDefault(x => x.AUTH == auth);
            var cf = db.CONFIRMAUTHs.FirstOrDefault(x => x.AUTH == auth);

            if (cf == null)
            {
                TempData["RoleError"] = "Permission record not found.";
                return RedirectToAction("Index");
            }

            if (user != null && user.USERNAME.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                TempData["RoleError"] = "Admin account cannot be deleted.";
                return RedirectToAction("Index");
            }

            string username = user != null ? user.USERNAME : "(no user)";
            string employeeCode = cf.CODE;

            if (user != null)
            {
                db.USERS.Remove(user);
            }
            db.CONFIRMAUTHs.Remove(cf);

            db.SaveChanges();

            LogHelper.WriteLog(
                db,
                "DeleteAuth",
                username,
                $"Permission {auth} for employee {employeeCode} has been deleted."
            );

            TempData["RoleSuccess"] = "Permission has been deleted.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateRole(string username, string roleGroup)
        {
            if (!CheckAccess.Role("AD"))
            {
                return Json(new { success = false, message = "No permission." });
            }

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(roleGroup))
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });

            var user = db.USERS.FirstOrDefault(x => x.USERNAME == username);
            if (user == null)
                return Json(new { success = false, message = "Không tìm thấy user." });

            var ca = db.CONFIRMAUTHs.FirstOrDefault(x => x.AUTH == user.AUTH);
            if (ca == null)
                return Json(new { success = false, message = "Không tìm thấy bản ghi xác thực." });

            string oldGroup = GetRoleGroup(ca.AUTH);
            if (string.Equals(oldGroup, roleGroup, StringComparison.OrdinalIgnoreCase))
            {
                return Json(new
                {
                    success = true,
                    roleName = GetRoleGroupName(roleGroup)
                });
            }

            string manv = ca.CODE;
            var nv = db.NHANVIENs.FirstOrDefault(x => x.MANV == manv);
            if (nv == null)
                return Json(new { success = false, message = "Không tìm thấy nhân viên tương ứng." });

            string newAuth = BuildNewAuth(ca.AUTH, roleGroup);

            ca.AUTH = newAuth;
            ca.NAMEAUTH = GetRoleGroupName(roleGroup);

            user.AUTH = newAuth;
            user.CONFIRMAUTH = ca;

            nv.MACV = MapRoleGroupToPosition(roleGroup);

            db.SaveChanges();

            return Json(new
            {
                success = true,
                roleName = ca.NAMEAUTH
            });
        }

        private static string GetRoleGroup(string authCode)
        {
            if (string.IsNullOrEmpty(authCode) || authCode.Length < 2)
                return authCode ?? "";
            return authCode.Substring(0, 2).ToUpperInvariant();
        }

        private static string GetRoleGroupName(string group)
        {
            group = (group ?? "").ToUpperInvariant();
            switch (group)
            {
                case "AD": return "Admin";
                case "HR": return "Human Resources";
                case "FN": return "Finance Manager";
                case "EM": return "Employee";
                default: return "Unknown";
            }
        }

        private static string BuildNewAuth(string oldAuth, string newGroup)
        {
            newGroup = (newGroup ?? "").ToUpperInvariant();
            if (string.IsNullOrEmpty(oldAuth))
                return newGroup;

            string tail = oldAuth.Length > 2 ? oldAuth.Substring(2) : "";
            return newGroup + tail;
        }

        private static string MapRoleGroupToPosition(string group)
        {
            group = (group ?? "").ToUpperInvariant();
            switch (group)
            {
                case "HR": return "CV_HR01";
                case "FN": return "CV_FN01";
                case "AD": return "CV_AD01";
                case "EM":
                default: return "CV_EM01";
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string username)
        {
            if (!CheckAccess.Role("AD"))
            {
                return Json(new { success = false, message = "No permission." });
            }

            if (string.IsNullOrWhiteSpace(username))
                return Json(new { success = false, message = "Username không hợp lệ." });

            var user = db.USERS.FirstOrDefault(x => x.USERNAME == username);
            if (user == null)
                return Json(new { success = false, message = "Không tìm thấy user." });

            var ca = db.CONFIRMAUTHs.FirstOrDefault(x => x.AUTH == user.AUTH);

            try
            {
                if (ca != null)
                {
                    db.CONFIRMAUTHs.Remove(ca);
                }

                db.USERS.Remove(user);
                db.SaveChanges();

                return Json(new { success = true, message = "Đã xoá tài khoản." });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Không xoá được tài khoản do còn ràng buộc dữ liệu."
                });
            }
        }

    }
}
